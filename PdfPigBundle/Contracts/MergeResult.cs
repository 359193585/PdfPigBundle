//MergeResult.cs

using System.Collections.Generic;

namespace PdfPigBundle.Contracts;
/// <summary>
/// merge result information, used to report the result of merging multiple PDF files.
/// </summary>
public class MergeResult
{
    public bool Success { get; set; }
    public int TotalPages { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>if ignoreDuplicates=true, list of duplicated files</summary>
    public List<string> DuplicatedFiles { get; set; } = new List<string>();
    /// <summary>list of actually merged files</summary>
    public List<string> MergedFiles { get; set; } = new List<string>();
    public IList<BookmarkEntry>? Bookmarks { get; set; }
}
