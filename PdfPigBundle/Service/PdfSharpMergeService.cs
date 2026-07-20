using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfPigBundle.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfPigBundle.Service
{
    /// <summary>
    /// 使用 PDFsharp 实现的 PDF 合并服务，支持书签（目录）保留和创建
    /// </summary>
    public class PdfSharpMergeService
    {
        /// <summary>
        /// 使用指定选项合并 PDF 文件
        /// </summary>
        public MergeResult Merge(string[] filePaths, string outputPath, MergeOptions options)
        {
            var result = new MergeResult { OutputPath = outputPath };

            try
            {
                // 1. 参数校验
                if (filePaths == null || filePaths.Length == 0)
                    throw new ArgumentException("至少提供一个文件路径");

                var validPaths = filePaths.Where(File.Exists).ToList();
                if (!validPaths.Any())
                    throw new FileNotFoundException("没有找到任何有效的 PDF 文件");

                // 2. 处理重复文件
                List<string> finalPaths;
                List<string>? duplicatedFiles = null;
                if (options.IgnoreDuplicates)
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

                
               

                // 4. 创建输出文档，并设置元数据
                using (var outputDocument = new PdfDocument())
                {
                    if (options.DocumentInfo != null)
                    {
                        outputDocument.Info.Title = options.DocumentInfo.Title;
                        outputDocument.Info.Author = options.DocumentInfo.Author;
                        outputDocument.Info.Subject = options.DocumentInfo.Subject;
                        outputDocument.Info.Creator = options.DocumentInfo.Creator;
                    }

                    // 设置默认值（如果用户没有提供）
                    if (string.IsNullOrEmpty(outputDocument.Info.Title))
                        outputDocument.Info.Title = "合并文档";
                    if (string.IsNullOrEmpty(outputDocument.Info.Author))
                        outputDocument.Info.Author = "PDFsharp合并工具";


                    int totalPages = 0;
                    int fileIndex = 0;
                    var fileBookmarkInfos = new List<FileBookmarkInfo>();

                    foreach (var path in finalPaths)
                    {
                        // 以 Import 模式打开，允许复制页面
                        using (var inputDocument = PdfReader.Open(path, PdfDocumentOpenMode.Import))
                        {
                            int pageCount = inputDocument.PageCount;
                            int startPage = totalPages + 1; // 该文件在输出文档中的起始页码（1-based）

                            // 记录书签信息
                            fileBookmarkInfos.Add(new FileBookmarkInfo
                            {
                                FilePath = path,
                                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(path),
                                StartPageNumber = startPage,
                                PageCount = pageCount
                            });

                            // 报告进度
                            options.Progress?.Report(new MergeProgress
                            {
                                FileIndex = fileIndex,
                                TotalFiles = finalPaths.Count,
                                FileName = Path.GetFileName(path),
                                PageCount = pageCount,
                                TotalPagesProcessed = totalPages,
                                IsComplete = false
                            });

                            // 复制所有页面到输出文档
                            foreach (PdfPage page in inputDocument.Pages)
                            {
                                outputDocument.AddPage(page);
                            }

                            totalPages += pageCount;
                            fileIndex++;
                        }
                    }

                    // 完成进度报告
                    options.Progress?.Report(new MergeProgress
                    {
                        FileIndex = finalPaths.Count,
                        TotalFiles = finalPaths.Count,
                        IsComplete = true,
                        TotalPagesProcessed = totalPages
                    });

                    result.TotalPages = totalPages;
                    result.Success = true;

                    // 5. 生成书签
                    if (options.BookmarkGenerator != null && fileBookmarkInfos.Any())
                    {
                        var bookmarkEntries = options.BookmarkGenerator.GenerateBookmarks(fileBookmarkInfos);
                        result.Bookmarks = bookmarkEntries;

                        // 使用 PDFsharp 的 Outlines.Add 方法添加书签
                        foreach (var entry in bookmarkEntries)
                        {
                            int pageIndex = entry.PageNumber - 1; // 页码从1开始，PDFsharp从0开始
                            if (pageIndex >= 0 && pageIndex < outputDocument.PageCount)
                            {
                                var destPage = outputDocument.Pages[pageIndex];
                                // 添加到根大纲（顶层书签）
                                outputDocument.Outlines.Add(entry.Title, destPage, false);
                                // 第三个参数 false 表示默认不展开子书签（顶层没有子书签，无关紧要）
                            }
                        }
                    }

                    // 保存文件
                    outputDocument.Save(outputPath);
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

        // ---------- 保留与原有服务相同的重载（直接转发） ----------

        public MergeResult Merge(string[] filePaths)
        {
            var output = Path.Combine(
                Path.GetDirectoryName(filePaths.First()) ?? string.Empty,
                "outputOfMerge.pdf");
            return Merge(filePaths, output, new MergeOptions());
        }

        public MergeResult Merge(string[] filePaths, string outputPath)
            => Merge(filePaths, outputPath, new MergeOptions());

        public MergeResult Merge(string[] filePaths, string outputPath, bool ignoreDuplicates)
            => Merge(filePaths, outputPath, new MergeOptions { IgnoreDuplicates = ignoreDuplicates });

        public MergeResult Merge(string[] filePaths, string outputPath,
            bool ignoreDuplicates = true,
            IProgress<MergeProgress>? progress = null)
        {
            var options = new MergeOptions
            {
                IgnoreDuplicates = ignoreDuplicates,
                Progress = progress
            };
            return Merge(filePaths, outputPath, options);
        }
    }
}
