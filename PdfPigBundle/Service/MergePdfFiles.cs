using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace PdfPigBundle.Service
{
    /// <summary>
    /// 合并进度信息
    /// </summary>
    public class MergeProgress
    {
        /// <summary>当前正在处理的文件索引（从0开始）</summary>
        public int FileIndex { get; set; }
        /// <summary>总文件数</summary>
        public int TotalFiles { get; set; }
        /// <summary>当前文件名</summary>
        public string? FileName { get; set; }
        /// <summary>当前文件的页数</summary>
        public int PageCount { get; set; }
        /// <summary>已处理的总页数</summary>
        public int TotalPagesProcessed { get; set; }
        /// <summary>是否已完成</summary>
        public bool IsComplete { get; set; }
        /// <summary>进度百分比（0-100），可选</summary>
        public double PercentComplete => TotalFiles > 0 ? (double)FileIndex / TotalFiles * 100 : 0;
    }

    /// <summary>
    /// 合并结果
    /// </summary>
    public class MergeResult
    {
        public bool Success { get; set; }
        public int TotalPages { get; set; }
        public string? OutputPath { get; set; }
        public string? ErrorMessage { get; set; }
        /// <summary>被忽略的重复文件列表（如果 ignoreDuplicates=true）</summary>
        public List<string> DuplicatedFiles { get; set; } = new List<string>();
        /// <summary>实际合并的文件列表</summary>
        public List<string> MergedFiles { get; set; } = new List<string>();
    }

    public class MergePdfFiles
    {
        public MergeResult Merge(
            string[] filePaths,
            string outputPath,
            bool ignoreDuplicates = true,
            IProgress<MergeProgress>? progress = null)
        {
            var result = new MergeResult { OutputPath = outputPath };

            try
            {
                // 1. 参数校验
                if (filePaths == null || filePaths.Length == 0)
                    throw new ArgumentException("至少提供一个文件路径");

                // 2. 文件有效性检查
                var validPaths = filePaths.Where(File.Exists).ToList();
                if (!validPaths.Any())
                    throw new FileNotFoundException("没有找到任何有效的 PDF 文件");

                // 3. 处理重复文件
                List<string> finalPaths;
                List<string>? duplicatedFiles = null;
                if (ignoreDuplicates)
                {
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    finalPaths = new List<string>();
                    duplicatedFiles = new List<string>();
                    foreach (var p in validPaths)
                    {
                        if (seen.Add(p))
                            finalPaths.Add(p);
                        else
                            duplicatedFiles.Add(p);
                    }
                }
                else
                {
                    finalPaths = validPaths;
                }

                result.DuplicatedFiles = duplicatedFiles ?? new List<string>();
                result.MergedFiles = finalPaths;

                // 4. 流式合并
                using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                using (var outputDocument = new PdfDocumentBuilder(outputStream))
                {
                    int totalPages = 0;
                    int fileIndex = 0;
                    foreach (var path in finalPaths)
                    {
                        using (var inputDocument = PdfDocument.Open(path))
                        {
                            int pageCount = inputDocument.NumberOfPages;
                            var prog = new MergeProgress
                            {
                                FileIndex = fileIndex,
                                TotalFiles = finalPaths.Count,
                                FileName = Path.GetFileName(path),
                                PageCount = pageCount,
                                TotalPagesProcessed = totalPages,
                                IsComplete = false
                            };
                            progress?.Report(prog);

                            for (int i = 1; i <= pageCount; i++)
                            {
                                outputDocument.AddPage(inputDocument, i);
                            }
                            totalPages += pageCount;
                            fileIndex++;
                        }
                    }

                    // 完成进度报告
                    progress?.Report(new MergeProgress
                    {
                        FileIndex = finalPaths.Count,
                        TotalFiles = finalPaths.Count,
                        IsComplete = true,
                        TotalPagesProcessed = totalPages
                    });

                    result.TotalPages = totalPages;
                    result.Success = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public MergeResult Merge(string[] filePaths, string outputPath, bool ignoreDuplicates = true)
        {
            return Merge(filePaths, outputPath, ignoreDuplicates);
        }

        public MergeResult Merge(string[] filePaths)
        {
            if (filePaths == null || filePaths.Length == 0)
                throw new ArgumentException("至少提供一个文件路径");

            var output = Path.Combine(
                Path.GetDirectoryName(filePaths.First()) ?? string.Empty,
                "outputOfMerge.pdf");

            return Merge(filePaths, output, ignoreDuplicates: true);
        }
    }
}