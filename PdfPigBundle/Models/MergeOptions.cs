//MergeOptions.cs

using System;
using UglyToad.PdfPig.Content;
using static UglyToad.PdfPig.Writer.PdfDocumentBuilder;

namespace PdfPigBundle.Models;
public class MergeOptions
{
    public bool IgnoreDuplicates { get; set; } = true;

    public DocumentInformationBuilder? DocumentInfo { get; set; }

    public IBookmarkGenerator? BookmarkGenerator { get; set; }

    public IProgress<MergeProgress>? Progress { get; set; }
}
