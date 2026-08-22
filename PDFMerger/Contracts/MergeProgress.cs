//MergeProgress.cs
namespace PdfMerger.Contracts;
/// <summary>
/// merge progress information, used to report the progress of merging multiple PDF files.
/// </summary>
public class MergeProgress
{
    public int FileIndex { get; set; }
    public int TotalFiles { get; set; }
    public string? FileName { get; set; }
    public int PageCount { get; set; }
    public int TotalPagesProcessed { get; set; }
    public bool IsComplete { get; set; }
    public double PercentComplete => TotalFiles > 0 ? (double)FileIndex / TotalFiles * 100 : 0;
}


