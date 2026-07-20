using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using PdfPigBundle.ViewModel;

namespace PdfPigBundle.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainWindowViewModel();
            DataContext = vm;
            ConfigureDataGridColumns();
            vm.ShowMessageRequested += async (s, msg) =>
            {
                var box = MessageBoxManager.GetMessageBoxStandard("提示", msg);
                await box.ShowAsync();
            };

        }
      
        private void ConfigureDataGridColumns()
        {
            FileDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "文件名",
                Binding = new Binding("FileName"),
                Width = new DataGridLength(3, DataGridLengthUnitType.Star)
            });
            FileDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "页数",
                Binding = new Binding("PageCount"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            FileDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "大小",
                Binding = new Binding("FileSizeDisplay"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            FileDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "作者",
                Binding = new Binding("Author"),
                Width = new DataGridLength(1.5, DataGridLengthUnitType.Star)
            });
        }
        // ---------- 添加文件 ----------
        private async void OnAddFilesClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = true,
                FileTypeFilter = new[] { new FilePickerFileType("PDF 文件") { Patterns = new[] { "*.pdf" } } }
            });

            if (files != null && files.Count > 0)
            {
                var paths = files.Select(f => f.Path.LocalPath).ToArray();
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.AddFiles(paths); // 调用 ViewModel 的同步方法
                }
            }
        }

        // ---------- 浏览输出路径  ----------
        private async void OnBrowseOutputClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择输出目录"
            });

            if (folder != null && folder.Count > 0)
            {
                var dir = folder[0].Path.LocalPath;
                if (DataContext is MainWindowViewModel vm)
                {
                    string fileName = string.IsNullOrEmpty(vm.OutputPath)
                        ? MainWindowViewModel.DefaultOutputPdfName
                        : System.IO.Path.GetFileName(vm.OutputPath);
                    vm.SetOutputPath(System.IO.Path.Combine(dir, fileName));
                }
            }
        }
        private async void OnAboutClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await App.ShowAboutDialogAsync();
        }

        // ---------- 拖放（DragOver） ----------
        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.DataTransfer.Formats.Contains(DataFormat.File))
                e.DragEffects = DragDropEffects.Copy;
            else
                e.DragEffects = DragDropEffects.None;
        }

        // ---------- 拖放（Drop） ----------
        private void OnDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.DataTransfer.Formats.Contains(DataFormat.File))
            {
                var files = e.DataTransfer.TryGetFiles();
                if (files != null && files.Any())
                {
                    var filePaths = files.Select(f => f.Path.LocalPath).ToArray();
                    if (DataContext is MainWindowViewModel vm)
                    {
                        // 调用 ViewModel 的同步方法（内部自动处理异步）
                        vm.AddFiles(filePaths);
                    }
                }
            }
        }
        private async void OnOpenFolderClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                if (string.IsNullOrEmpty(vm.OutputPath))
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("提示", "输出路径为空，请先设置输出路径。");
                    await box.ShowAsync();
                    return;
                }

                var directory = System.IO.Path.GetDirectoryName(vm.OutputPath);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("提示", "输出目录不存在。");
                    await box.ShowAsync();
                    return;
                }
                else
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(startInfo);
                }
            }

           
        }
    }
}
