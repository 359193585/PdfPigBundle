//MergeOptions.cs

using System;
using System.Threading;

namespace PdfMerger.Contracts;
public class MergeOptions
{
    public bool IgnoreDuplicates { get; set; } = true;

    public string? Author { get; set; }
    public string? Title { get; set; }
    public string? Subject { get; set; } = "";
    public string? Creator { get; set; } = "";

    public IBookmarkGenerator? BookmarkGenerator { get; set; }

    public IProgress<MergeProgress>? Progress { get; set; }

    public bool AddPageNumbers { get; set; } = false;

    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}
