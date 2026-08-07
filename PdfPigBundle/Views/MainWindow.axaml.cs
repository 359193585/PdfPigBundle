using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using PdfPigBundle.Models;
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
            var vm = DataContext as MainWindowViewModel;
            if (vm == null) return;

            var filters = new List<FilePickerFileType>
            {
                new FilePickerFileType("PDF 文件")
                {
                    Patterns = new[] { "*.pdf" }
                }
            };

            if (vm.EnableImageSupport)
            {
                filters.Add(new FilePickerFileType("图片文件")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.tiff" }
                });
                // 加一个“所有支持的文件”选项
                filters.Add(new FilePickerFileType("所有支持的文件")
                {
                    Patterns = new[] { "*.pdf", "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.tiff" }
                });
            }
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = true,
                FileTypeFilter = filters
            });

            if (files != null && files.Count > 0)
            {
                var paths = files.Select(f => f.Path.LocalPath).ToArray();
                vm.AddFiles(paths); 
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

        // ---------- 关于  ----------
        private async void OnAboutClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await App.ShowAboutDialogAsync();
        }

        // ---------- file DragOver ----------
        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.DataTransfer.Formats.Contains(DataFormat.File))
                e.DragEffects = DragDropEffects.Copy;
            else
                e.DragEffects = DragDropEffects.None;
        }

        // ---------- file Drop ----------
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

                var directory = Path.GetDirectoryName(vm.OutputPath);
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

        #region Drag and Drop for DataGrid
        //private async void OnDataGridPointerPressed(object? sender, PointerPressedEventArgs e)
        //{
        //    var point = e.GetCurrentPoint(sender as Visual);
        //    if (point.Properties.IsLeftButtonPressed)
        //    {
        //        var dataGrid = sender as DataGrid;
        //        if (dataGrid?.SelectedItem is FileItem fileItem)
        //        {
        //            // 使用 DataTransfer 存储数据
        //           // var data = new DataTransfer();
        //            var data = new DataObject();
        //            data.Set("FileItem", fileItem);

        //            // 获取 TopLevel 并启动拖放
        //            var topLevel = TopLevel.GetTopLevel(dataGrid);
        //            if (topLevel != null)
        //            {
        //                // 异步启动拖放，并等待结果（可选）
        //                var result = await topLevel.DragDrop.StartAsync(data, DragDropEffects.Move);
        //                // 可以根据 result 执行后续操作（如改变光标等）
        //            }
        //        }
        //    }

        //}
        //private void OnDataGridDragOver(object sender, DragEventArgs e)
        //{
        //    e.Handled = true;
        //    // 使用 Any() 和 ToString() 比较
        //    if (e.DataTransfer.Formats.Any(f => f == DataFormat.File))
        //        e.DragEffects = DragDropEffects.Copy;
        //    else if (e.DataTransfer.Formats.Any(f => f.ToString() == "FileItem"))
        //        e.DragEffects = DragDropEffects.Move;
        //    else
        //        e.DragEffects = DragDropEffects.None;
        //}

        //private void OnDataGridDrop(object sender, DragEventArgs e)
        //{
        //    e.Handled = true;

        //    //  外部文件拖放
        //    if (e.DataTransfer.Formats.Contains(DataFormat.File))
        //    {
        //        var files = e.DataTransfer.TryGetFiles();
        //        if (files != null && files.Any())
        //        {
        //            var filePaths = files.Select(f => f.Path.LocalPath).ToArray();
        //            if (DataContext is MainWindowViewModel vm)
        //            {
        //                vm.AddFiles(filePaths);
        //            }
        //        }
        //        return; 
        //    }

        //    // 内部行拖拽
        //    if (e.DataTransfer.Formats.Any(f => f.ToString() == "FileItem"))
        //    {
        //        var draggedItem = e.DataTransfer.Get("FileItem") as FileItem;
        //        if (draggedItem == null) return;

        //        var targetRow = FindParent<DataGridRow>(e.Source as Visual);
        //        if (targetRow?.DataContext is FileItem targetItem)
        //        {
        //            var vm = DataContext as MainWindowViewModel;
        //            vm?.MoveFileItem(draggedItem, targetItem);
        //        }
        //    }
        //}
       

        //private static T? FindParent<T>(Visual? visual) where T : Visual
        //{
        //    while (visual != null)
        //    {
        //        if (visual is T t)
        //            return t;
        //        visual = visual.Parent as Visual;
        //    }
        //    return null;
        //}
        #endregion
    }
}
