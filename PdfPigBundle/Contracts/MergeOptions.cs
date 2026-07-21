//MergeOptions.cs

using System;

namespace PdfPigBundle.Contracts;
public class MergeOptions
{
    public bool IgnoreDuplicates { get; set; } = true;

    public string? Author { get; set; }
    public string? Title { get; set; }
    public string? Subject { get; set; } = "";
    public string? Creator { get; set; } = "";

    public IBookmarkGenerator? BookmarkGenerator { get; set; }

    public IProgress<MergeProgress>? Progress { get; set; }
}
