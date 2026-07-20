using Avalonia.Controls;
using Avalonia.Interactivity;
using PdfPigBundle.Models;
using PdfPigBundle.Service;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PdfPigBundle.Views
{
    public partial class MainWindowOld : Window
    {
        public MainWindowOld()
        {
            InitializeComponent();
        }

        private async void OnOkClickAsync(object? sender, RoutedEventArgs e)
        {
            // 禁用按钮，防止重复点击
            if (sender is not Button button)
            {
                StatusText.Text = "未能识别按钮控件。";
                return;
            }
            button.IsEnabled = false; 

            StatusText.Text = "准备合并...";
            ProgressBar.Value = 0;

            try
            {
                string[] fileList = GetFilesList();
                var merger = new MergePdfFiles();

                // 进度报告，更新 UI
                var progress = new Progress<MergeProgress>(p =>
                {
                    // 注意：Progress<T> 的回调默认在 UI 线程执行（因为捕获了 SynchronizationContext）
                    if (p.IsComplete)
                    {
                        StatusText.Text = $"✅ 合并完成！共 {p.TotalPagesProcessed} 页";
                        ProgressBar.Value = 100;
                    }
                    else
                    {
                        StatusText.Text = $"📄 正在合并 [{p.FileIndex + 1}/{p.TotalFiles}]: {p.FileName} ({p.PageCount} 页)";
                        ProgressBar.Value = p.PercentComplete; // 使用之前定义的 PercentComplete
                    }
                });

                var result = await Task.Run(() =>
                    merger.Merge(
                        fileList,
                        "output.pdf",
                        ignoreDuplicates: false,
                        progress: progress)
                );

                if (result.Success)
                {
                    StatusText.Text = $"成功！总页数：{result.TotalPages}，输出文件：{result.OutputPath}";
                    if (result.DuplicatedFiles.Any())
                        StatusText.Text += $"\n忽略重复文件：{string.Join(", ", result.DuplicatedFiles)}";
                }
                else
                {
                    StatusText.Text = $"合并失败：{result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"发生异常：{ex.Message}";
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private static string[] GetFilesList()
        {
            return new string[] {
            "E:\\temp\\TestPdfFile\\这是Page 001.pdf",
            "E:\\temp\\TestPdfFile\\这是Page 002.pdf",
            "E:\\temp\\TestPdfFile\\这是Page 003.pdf",
            "E:\\temp\\TestPdfFile\\clr-via-csharp（第4版）.pdf",
            "E:\\temp\\TestPdfFile\\clr-via-csharp（第4版）.pdf",
            "E:\\temp\\TestPdfFile\\clr-via-csharp（第4版）.pdf",
            "E:\\temp\\TestPdfFile\\clr-via-csharp（第4版）.pdf",
            "E:\\temp\\TestPdfFile\\clr-via-csharp（第4版）.pdf"
            };
        }
    }
}
