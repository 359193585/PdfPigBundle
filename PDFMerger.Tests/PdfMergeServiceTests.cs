using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfMerger.Contracts;
using PdfMerger.Service;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Xunit;

namespace PDFMerger.Tests;

public class PdfMergeServiceTests : IDisposable
{
    private readonly PdfSharpMergeService _pdfMergeService;
    private readonly string _testDirectory;

    public PdfMergeServiceTests()
    {
        _pdfMergeService = new PdfSharpMergeService();

        _testDirectory = Path.Combine(Path.GetTempPath(), "PdfMergeTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    /// <summary>
    /// 测试正常合并流程：验证返回 Success、页数统计正确且落盘文件合法。
    /// </summary>
    [Fact]
    public async Task MergeAsync_ValidInputs_ReturnsSuccessAndCreatesFile()
    {
        // Arrange
        string input1 = CreateDummyPdfFile("test1.pdf", pages: 2);
        string input2 = CreateDummyPdfFile("test2.pdf", pages: 3);
        string outputPath = Path.Combine(_testDirectory, "output_success.pdf");

        // Act
        var result = await _pdfMergeService.MergeAsync(
            new[] { input1, input2 },
            outputPath,
            new MergeOptions());

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"预期合并成功，但失败信息为: {result.ErrorMessage}");
        Assert.Equal(5, result.TotalPages);
        Assert.True(File.Exists(outputPath), "目标合并文件应在磁盘生成。");
        Assert.True(new FileInfo(outputPath).Length > 0, "合并文件大小应大于 0 字节。");
    }

    /// <summary>
    /// 测试中途取消：验证捕获 Cancel 信号后抛出 OperationCanceledException，
    /// 并且物理磁盘上不残留半成品或 0 字节文件。
    /// </summary>
    [Fact]
    public async Task MergeAsync_WhenCancelled_ThrowsOperationCanceledExceptionAndDeletesOutputFile()
    {
        // Arrange
        // 创建较多页数的 PDF 文件，确保耗时循环中有足够时间接收取消信号
        string input1 = CreateDummyPdfFile("large1.pdf", pages: 100);
        string input2 = CreateDummyPdfFile("large2.pdf", pages: 100);
        string outputPath = Path.Combine(_testDirectory, "output_cancelled.pdf");

        using var cts = new CancellationTokenSource();

        // Act & Assert
        // 1. 发起异步合并任务
        var mergeTask = _pdfMergeService.MergeAsync(
            new[] { input1, input2 },
            outputPath,
            new MergeOptions(),
            cts.Token);

        // 2. 模拟中途发出 Cancel 指令
        cts.Cancel();

        // 3. 验证方法是否向上抛出了 OperationCanceledException
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await mergeTask);

        // 4. 核心断言：物理磁盘上绝对不能残留任何文件
        Assert.False(File.Exists(outputPath), "取消操作后，未合并完成的输出文件必须被自动清理剔除。");
    }

    /// <summary>
    /// 测试无效输入：验证文件不存在时的错误捕获。
    /// </summary>
    [Fact]
    public async Task MergeAsync_FileNotFound_ReturnsFailureResult()
    {
        // Arrange
        string nonExistentFile = Path.Combine(_testDirectory, "non_existent.pdf");
        string outputPath = Path.Combine(_testDirectory, "output_fail.pdf");

        // Act
        var result = await _pdfMergeService.MergeAsync(
            new[] { nonExistentFile },
            outputPath,
            new MergeOptions());

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        Assert.False(File.Exists(outputPath), "输入无效时不应生成目标文件。");
    }

    #region Helpers & Cleanup
      
    private string CreateDummyPdfFile(string fileName, int pages)
    {
        string filePath = Path.Combine(_testDirectory, fileName);

        using (var document = new PdfDocument())
        {
            for (int i = 0; i < pages; i++)
            {
                document.AddPage();
            }
            document.Save(filePath);
        }

        return filePath;
    }
       
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
        }
    }

    #endregion
}
