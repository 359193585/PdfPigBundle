//MergeResult.cs

using System.Collections.Generic;

namespace PdfPigBundle.Contracts;
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
    public IList<BookmarkEntry>? Bookmarks { get; set; }
}
