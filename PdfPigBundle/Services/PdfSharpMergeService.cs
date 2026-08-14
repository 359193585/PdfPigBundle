//PdfSharpMergeService.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Lang.Avalonia;
using PdfPigBundle.Contracts;
using PdfPigBundle.Services;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfPigBundle.Service
{
    /// <summary>
    /// PDF merging service implemented with PDFsharp, supporting retention of original bookmarks and using file names as first-level directories
    /// </summary>
    public class PdfSharpMergeService
    {
        /// <summary>
        /// Merges PDF files using the specified options, core method, returns the merge result
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
                    outputDocument.Info.Title = options.Title ?? "MergeredFiles";
                    outputDocument.Info.Author = options.Author ?? "User of PDFMerger";
                    outputDocument.Info.Subject = options.Subject ?? "";
                    outputDocument.Info.Creator = options.Creator ?? "PDFMerger";

                    var context = new MergeContext(outputDocument, finalPaths, options)
                    {
                        // Used to store merge information for each file, only assigned for processed files
                        FileInfos = new List<FileMergeInfo>(),
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

                    // Report completion progress
                    options.Progress?.Report(new MergeProgress
                    {
                        FileIndex = finalPaths.Count,
                        TotalFiles = finalPaths.Count,
                        IsComplete = true,
                        TotalPagesProcessed = context.TotalPages
                    });


                    result.TotalPages = context.TotalPages;
                    result.Success = true;

                    //  Generate bookmarks (if a generator is provided or the original document has bookmarks)
                    if (options.BookmarkGenerator != null || context.FileInfos.Any(f => f.OutlineNodes.Any()))
                    {
                        GenerateBookmarks(outputDocument, context.FileInfos);
                    }

                    // After all pages are added, check if page numbers need to be added
                    if (options.AddPageNumbers && result.TotalPages > 0)
                    {
                        AddPageNumbers(outputDocument);
                    }
                    outputDocument.Save(outputPath);
                    return result;
                }
            }
            catch (NotImplementedException ex) when (ex.Message.Contains(">2GiB"))
            {
                // Specifically handle errors for files larger than 2GB
                result.Success = false;
                result.ErrorMessage = T("Error_BiggerThanMaxSize", "");
                Debug.WriteLine($"Large file error: {ex}");
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.ToString();
                return result;
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
        private void AddPageNumbers(PdfDocument document)
        {
            int totalPages = document.PageCount;
            // Use standard Helvetica font
            GlobalFontSettings.FontResolver = new CustomFontResolver();
            XFont font = new XFont("Helvetica", 12, XFontStyleEx.Regular);
            XBrush brush = XBrushes.Black;

            for (int i = 0; i < totalPages; i++)
            {
                PdfPage page = document.Pages[i];
                // Open the page in append mode for drawing
                using (XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
                {
                    string text = $"{i + 1} / {totalPages}";
                    XSize size = gfx.MeasureString(text, font);
                    // Centered at the bottom, 20 points from the bottom
                    double pageWidth = page.Width.Point;
                    double pageHeight = page.Height.Point;
                    double x = (pageWidth - size.Width) / 2;
                    double y = pageHeight - 20;
                    gfx.DrawString(text, font, brush, x, y);
                }
            }
        }
        private void GenerateBookmarks(PdfDocument outputDocument, List<FileMergeInfo> fileInfos)
        {
            foreach (var fileInfo in fileInfos)
            {
                // Create top-level bookmark (use file name)
                int firstPageIndex = fileInfo.StartPageNumber - 1;
                if (firstPageIndex >= 0 && firstPageIndex < outputDocument.PageCount)
                {
                    var destPage = outputDocument.Pages[firstPageIndex];
                    var fileOutline = outputDocument.Outlines.Add(fileInfo.FileNameWithoutExtension, destPage, false);

                    // If the file has original bookmarks, add them as child bookmarks
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
                    // Images have no child bookmarks, pass an empty list
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

            // Record file information (used for bookmarks)
            var fileInfo = new FileMergeInfo
            {
                FilePath = filePath,
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath),
                StartPageNumber = startPage,
                PageCount = pageCount,
                OutlineNodes = outlineNodes ?? new List<OutlineNode>()
            };
            context.FileInfos.Add(fileInfo);

            // report progress but not complete yet
            context.Options.Progress?.Report(new MergeProgress
            {
                FileIndex = context.FileIndex,
                TotalFiles = context.FinalPaths.Count,
                FileName = Path.GetFileName(filePath),
                PageCount = pageCount,
                TotalPagesProcessed = context.TotalPages,
                IsComplete = false
            });

            // Copy pages to the output document
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
                throw new ArgumentException("please provide at least one file path");

            var validPaths = filePaths.Where(File.Exists).ToList();
            if (!validPaths.Any())
                throw new FileNotFoundException("no valid PDF files were found");
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

        // ---------- Helper Method: Extract Outline Tree ----------
        private List<OutlineNode> ExtractOutlineNodes(PdfOutlineCollection outlines, Dictionary<PdfPage, int> pageIndexMap)
        {
            var list = new List<OutlineNode>();
            if (outlines == null) return list;
            foreach (PdfOutline outline in outlines)
            {
                list.Add(ExtractOutlineNode(outline, pageIndexMap)); // Pass the mapping
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
                node.Children.Add(ExtractOutlineNode(child, pageIndexMap)); // Recursively pass
            }
            return node;
        }

        // ---------- Helper Method: Add Outline Node to Output Document ----------
        private void AddOutlineNode(OutlineNode node, PdfOutline parent, int pageOffset, PdfDocument outputDoc)
        {
            int destPageIndex = node.PageIndex + pageOffset;
            if (destPageIndex < 0 || destPageIndex >= outputDoc.PageCount)
                return; // Skip invalid page numbers

            var destPage = outputDoc.Pages[destPageIndex];
            // Create outline node (expanded state depends on whether it has child nodes)
            var newOutline = parent.Outlines.Add(node.Title, destPage, node.Children.Any());
            // Recursively add child nodes
            foreach (var child in node.Children)
            {
                AddOutlineNode(child, newOutline, pageOffset, outputDoc);
            }
        }
        private static string T(string key, params object[] args)
        {
            var value = I18nManager.Instance.GetResource(key);
            if (string.IsNullOrEmpty(value))
                return key;
            return args.Length > 0 ? string.Format(value, args) : value;
        }

        #region  ---------- Merge Overload Methods ----------
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
        #endregion

    }

    #region Helper Data Structures 
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
    #endregion

}
