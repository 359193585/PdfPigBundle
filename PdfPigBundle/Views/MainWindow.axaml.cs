using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
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


            // ----- 为 DataGrid 启用拖放功能  -----
            DragDrop.SetAllowDrop(FileDataGrid, true);

            FileDataGrid.AddHandler(PointerPressedEvent, OnDataGridPointerPressed, RoutingStrategies.Tunnel);
            FileDataGrid.AddHandler(PointerMovedEvent, OnDataGridPointerMoved, RoutingStrategies.Tunnel);
            FileDataGrid.AddHandler(PointerReleasedEvent, OnDataGridPointerReleased, RoutingStrategies.Tunnel);
            FileDataGrid.AddHandler(DragDrop.DragOverEvent, OnDataGridDragOver);
            FileDataGrid.AddHandler(DragDrop.DropEvent, OnDataGridDrop);


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
        private static readonly DataFormat<FileItem> FileItemFormat =
     DataFormat.CreateInProcessFormat<FileItem>("FileItem");
        private Point? _dragStartPoint;
        private FileItem? _dragItem;
        private PointerPressedEventArgs? _dragPressedArgs;
        private void OnDataGridPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed)
            {
                _dragStartPoint = e.GetPosition(sender as Visual);
                _dragItem = (sender as DataGrid)?.SelectedItem as FileItem;
                _dragPressedArgs = e;  // 保存事件参数
                Debug.WriteLine($"  Selected: {_dragItem?.FileName}");
            }
        }
        private async void OnDataGridPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_dragStartPoint == null || _dragItem == null || _dragPressedArgs == null)
                return;

            var currentPos = e.GetPosition(sender as Visual);
            if (Math.Abs(currentPos.X - _dragStartPoint.Value.X) > 5 ||
                Math.Abs(currentPos.Y - _dragStartPoint.Value.Y) > 5)
            {
                var dragData = new DataTransfer();
                dragData.Add(DataTransferItem.Create(FileItemFormat, _dragItem));
                // 使用保存的 _dragPressedArgs 作为第一个参数
                await DragDrop.DoDragDropAsync(_dragPressedArgs, dragData, DragDropEffects.Move);
                Debug.WriteLine("Drag finished");

                // 重置状态
                _dragStartPoint = null;
                _dragItem = null;
                _dragPressedArgs = null;
            }
        }

        private void OnDataGridPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            Debug.WriteLine("PointerReleased");
            _dragStartPoint = null;
            _dragItem = null;
            _dragPressedArgs = null;
        }
        private void OnDataGridDragOver(object? sender, DragEventArgs e)
        {
            Debug.WriteLine("DragOver");
            e.Handled = true;

            var formats = e.DataTransfer.Formats;
            if (formats.Contains(DataFormat.File))
                e.DragEffects = DragDropEffects.Copy;
            else if (formats.Contains(FileItemFormat))  
                e.DragEffects = DragDropEffects.Move;
            else
                e.DragEffects = DragDropEffects.None;

        }

        private void OnDataGridDrop(object? sender, DragEventArgs e)
        {
            Debug.WriteLine("Drop");
            e.Handled = true;

            //  外部文件拖放
            if (e.DataTransfer.Formats.Contains(DataFormat.File))
            {
                Debug.WriteLine("  Dropped files");
                var files = e.DataTransfer.TryGetFiles();
                if (files != null && files.Any())
                {
                    var filePaths = files.Select(f => f.Path.LocalPath).ToArray();
                    if (DataContext is MainWindowViewModel vm)
                    {
                        vm.AddFiles(filePaths);
                    }
                }
                return;
            }

            // 内部行拖拽
            var draggedItem = e.DataTransfer.TryGetValue<FileItem>(FileItemFormat);
            if (draggedItem != null)
            {
                Debug.WriteLine($"  Dragged item: {draggedItem.FileName}");
                var targetRow = FindParent<DataGridRow>(e.Source as Visual);
                if (targetRow?.DataContext is FileItem targetItem)
                {
                    Debug.WriteLine($"  Target item: {targetItem.FileName}");
                    var vm = DataContext as MainWindowViewModel;
                    vm?.MoveFileItem(draggedItem, targetItem);
                }
                else
                {
                    Debug.WriteLine("  Target row not found or DataContext not FileItem");
                }
            }
            else
            {
                Debug.WriteLine("  draggedItem is null");
            }
        }


        private static T? FindParent<T>(Visual? visual) where T : Visual
        {
            while (visual != null)
            {
                if (visual is T t)
                    return t;
                visual = visual.Parent as Visual;
            }
            return null;
        }
        #endregion
    }
}
