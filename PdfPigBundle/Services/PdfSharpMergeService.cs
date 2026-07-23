//PdfSharpMergeService.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfPigBundle.Contracts;
using PdfPigBundle.Services;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfPigBundle.Service
{
    /// <summary>
    /// 使用 PDFsharp 实现的 PDF 合并服务，支持保留原书签并以文件名作为一级目录
    /// </summary>
    public class PdfSharpMergeService
    {
        /// <summary>
        /// 使用指定选项合并 PDF 文件，核心方法，返回合并结果
        /// </summary>
        public MergeResult Merge(string[] filePaths, string outputPath, MergeOptions options)
        {
            var result = new MergeResult { OutputPath = outputPath };

            try
            {
                var finalPaths = CheckFilesStatus(filePaths, options, result);

                using (var outputDocument = new PdfDocument())
                {
                    // 设置文档信息
                    outputDocument.Info.Title = options.Title ?? "合并的文档";
                    outputDocument.Info.Author = options.Author ?? "PDFMerger用户";
                    outputDocument.Info.Subject = options.Subject ?? "";
                    outputDocument.Info.Creator = options.Creator ?? "PDFMerger";

                    var context = new MergeContext(outputDocument, finalPaths, options)
                    {
                        FileInfos = new List<FileMergeInfo>(), // 用于存储每个文件的合并信息，处理过文件才有赋值
                        TotalPages = 0,
                        FileIndex = 0
                    };

                    foreach (var path in finalPaths)
                    {
                        string ext = Path.GetExtension(path).ToLower();
                        bool isImage = (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif" || ext == ".tiff");

                        if (isImage)
                        {
                            ProcessSingleImage(context, path);
                        }
                        else
                        {
                            ProcessSingleFile(context, path);
                        }
                    }

                    // 报告完成进度
                    options.Progress?.Report(new MergeProgress
                    {
                        FileIndex = finalPaths.Count,
                        TotalFiles = finalPaths.Count,
                        IsComplete = true,
                        TotalPagesProcessed = context.TotalPages
                    });


                    result.TotalPages = context.TotalPages;
                    result.Success = true;

                    // 5. 生成书签（如果提供了生成器或原始文档有书签）
                    if (options.BookmarkGenerator != null || context.FileInfos.Any(f => f.OutlineNodes.Any()))
                    {
                        GenerateBookmarks(outputDocument, context.FileInfos);
                    }

                    // 所有页面添加完成后，检查是否需要添加页码
                    if (options.AddPageNumbers && result.TotalPages > 0)
                    {
                        AddPageNumbers(outputDocument);
                    }
                    // 保存文件
                    outputDocument.Save(outputPath);
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.ToString();
                return result;
            }
        }
        private void AddPageNumbers(PdfDocument document)
        {
            int totalPages = document.PageCount;
            // 使用标准 Helvetica 字体
            GlobalFontSettings.FontResolver = new CustomFontResolver();
            XFont font = new XFont("Helvetica", 12, XFontStyleEx.Regular);
            XBrush brush = XBrushes.Black;

            for (int i = 0; i < totalPages; i++)
            {
                PdfPage page = document.Pages[i];
                // 以追加模式打开页面进行绘制
                using (XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
                {
                    string text = $"{i + 1} / {totalPages}";
                    XSize size = gfx.MeasureString(text, font);
                    // 底部居中，距底部 20 点
                    double pageWidth = page.Width.Point;
                    double pageHeight = page.Height.Point;
                    double x = (pageWidth - size.Width) / 2;
                    double y = pageHeight - 20; // 距底部 20 点
                    gfx.DrawString(text, font, brush, x, y);
                }
            }
        }
        private class MergeContext
        {
            public MergeContext(PdfDocument outputDocument, List<string> finalPaths, MergeOptions options)
            {
                OutputDocument = outputDocument ?? throw new ArgumentNullException(nameof(outputDocument));
                FinalPaths = finalPaths ?? throw new ArgumentNullException(nameof(finalPaths));
                Options = options ?? throw new ArgumentNullException(nameof(options));
                FileInfos = new List<FileMergeInfo>();
            }
            public PdfDocument OutputDocument { get; set; }
            public List<FileMergeInfo> FileInfos { get; set; }
            public List<string> FinalPaths { get; set; }
            public MergeOptions Options { get; set; }
            public int TotalPages { get; set; }
            public int FileIndex { get; set; }
        }
        private void GenerateBookmarks(PdfDocument outputDocument, List<FileMergeInfo> fileInfos)
        {
            foreach (var fileInfo in fileInfos)
            {
                // 创建顶层书签（文件名）
                int firstPageIndex = fileInfo.StartPageNumber - 1;
                if (firstPageIndex >= 0 && firstPageIndex < outputDocument.PageCount)
                {
                    var destPage = outputDocument.Pages[firstPageIndex];
                    var fileOutline = outputDocument.Outlines.Add(fileInfo.FileNameWithoutExtension, destPage, false);

                    // 如果该文件有原始书签，则将其作为子书签添加
                    if (fileInfo.OutlineNodes.Any())
                    {
                        foreach (var rootNode in fileInfo.OutlineNodes)
                        {
                            AddOutlineNode(rootNode, fileOutline, fileInfo.StartPageNumber - 1, outputDocument);
                        }
                    }
                }
            }
        }

        private void ProcessSingleImage(MergeContext context, string imagePath)
        {
            var converter = new ImageToPdfPageConverter();
            using (var imgDoc = converter.ConvertImageToPdfDocument(imagePath, ImageToPdfPageConverter.PageSizeMode.A4))
            using (var ms = new MemoryStream())
            {
                imgDoc.Save(ms);
                ms.Position = 0;
                using (var importDoc = PdfReader.Open(ms, PdfDocumentOpenMode.Import))
                {
                    var pages = importDoc.Pages.Cast<PdfPage>();
                    // 图片无子书签，传空列表
                    ProcessPages(context, imagePath, pages, importDoc.PageCount, new List<OutlineNode>());
                }
            }
        }
        private void ProcessSingleFile(MergeContext context, string path)
        {

            using (var inputDocument = PdfReader.Open(path, PdfDocumentOpenMode.Import))
            {
                var pageIndexMap = new Dictionary<PdfPage, int>();
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    pageIndexMap[inputDocument.Pages[i]] = i;
                }

                int pageCount = inputDocument.PageCount;
                int startPage = context.TotalPages + 1; // 1-based

                var outlineNodes = ExtractOutlineNodes(inputDocument.Outlines, pageIndexMap);

                var pages = inputDocument.Pages.Cast<PdfPage>();

                ProcessPages(context, path, pages, inputDocument.PageCount, outlineNodes);
            }
        }

        private void ProcessPages(
                    MergeContext context,
                    string filePath,
                    IEnumerable<PdfPage> pages,
                    int pageCount,
                    List<OutlineNode>? outlineNodes = null)
        {
            int startPage = context.TotalPages + 1;

            // 记录文件信息（用于书签）
            var fileInfo = new FileMergeInfo
            {
                FilePath = filePath,
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath),
                StartPageNumber = startPage,
                PageCount = pageCount,
                OutlineNodes = outlineNodes ?? new List<OutlineNode>()
            };
            context.FileInfos.Add(fileInfo);

            // 报告当前文件处理进度（非完成）
            context.Options.Progress?.Report(new MergeProgress
            {
                FileIndex = context.FileIndex,
                TotalFiles = context.FinalPaths.Count,
                FileName = Path.GetFileName(filePath),
                PageCount = pageCount,
                TotalPagesProcessed = context.TotalPages,
                IsComplete = false
            });

            // 复制页面到输出文档
            foreach (var page in pages)
            {
                context.OutputDocument.AddPage(page);
            }

            context.TotalPages += pageCount;
            context.FileIndex++;
        }
        private List<string> CheckFilesStatus(string[] filePaths, MergeOptions options, MergeResult result)
        {
            if (filePaths == null || filePaths.Length == 0)
                throw new ArgumentException("至少提供一个文件路径");

            var validPaths = filePaths.Where(File.Exists).ToList();
            if (!validPaths.Any())
                throw new FileNotFoundException("没有找到任何有效的 PDF 文件");

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
            return finalPaths;
        }

        // ---------- 辅助方法：提取大纲树 ----------
        private List<OutlineNode> ExtractOutlineNodes(PdfOutlineCollection outlines, Dictionary<PdfPage, int> pageIndexMap)
        {
            var list = new List<OutlineNode>();
            if (outlines == null) return list;
            foreach (PdfOutline outline in outlines)
            {
                list.Add(ExtractOutlineNode(outline, pageIndexMap)); // 传递映射
            }
            return list;
        }

        private OutlineNode ExtractOutlineNode(PdfOutline outline, Dictionary<PdfPage, int> pageIndexMap)
        {
            var node = new OutlineNode
            {
                Title = outline.Title,
                PageIndex = outline.DestinationPage != null && pageIndexMap.TryGetValue(outline.DestinationPage, out int idx)
                    ? idx
                    : -1
            };
            foreach (PdfOutline child in outline.Outlines)
            {
                node.Children.Add(ExtractOutlineNode(child, pageIndexMap)); // 递归传递
            }
            return node;
        }

        // ---------- 辅助方法：添加大纲节点到输出文档 ----------
        private void AddOutlineNode(OutlineNode node, PdfOutline parent, int pageOffset, PdfDocument outputDoc)
        {
            int destPageIndex = node.PageIndex + pageOffset;
            if (destPageIndex < 0 || destPageIndex >= outputDoc.PageCount)
                return; // 无效页码则跳过

            var destPage = outputDoc.Pages[destPageIndex];
            // 创建大纲节点（展开状态取决于是否有子节点）
            var newOutline = parent.Outlines.Add(node.Title, destPage, node.Children.Any());
            // 递归添加子节点
            foreach (var child in node.Children)
            {
                AddOutlineNode(child, newOutline, pageOffset, outputDoc);
            }
        }

        // ---------- Merge 重载方法  ----------
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

    // ---------- 辅助数据结构 ----------
    public class FileMergeInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileNameWithoutExtension { get; set; } = string.Empty;
        public int StartPageNumber { get; set; } // 1-based
        public int PageCount { get; set; }
        public List<OutlineNode> OutlineNodes { get; set; } = new List<OutlineNode>();
    }

    public class OutlineNode
    {
        public string Title { get; set; } = string.Empty;
        public int PageIndex { get; set; } // 0-based within source
        public List<OutlineNode> Children { get; set; } = new List<OutlineNode>();

       
    }

}
