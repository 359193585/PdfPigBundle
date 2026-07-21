//MergeProgress.cs
namespace PdfPigBundle.Contracts;
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


