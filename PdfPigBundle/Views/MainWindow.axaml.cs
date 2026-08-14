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
using Lang.Avalonia;
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
                var box = MessageBoxManager.GetMessageBoxStandard("Notice", msg);
                await box.ShowAsync();
            };


            // ----- enable drag and drop for DataGrid  -----
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
                Header = T("DataGrid_Column_FileName"),
                Binding = new Binding("FileName"),
                Width = new DataGridLength(3, DataGridLengthUnitType.Star)
            });
            FileDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = T("DataGrid_Column_PageCount"),
                Binding = new Binding("PageCount"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            FileDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = T("DataGrid_Column_FileSize"),
                Binding = new Binding("FileSizeDisplay"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            FileDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = T("DataGrid_Column_Author"),
                Binding = new Binding("Author"),
                Width = new DataGridLength(1.5, DataGridLengthUnitType.Star)
            });
        }
        private async void OnAddFilesClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm == null) return;

            var filters = new List<FilePickerFileType>
            {
                new FilePickerFileType("PDF Files")
                {
                    Patterns = new[] { "*.pdf" }
                }
            };

            if (vm.EnableImageSupport)
            {
                filters.Add(new FilePickerFileType("Image Files")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.tiff" }
                });
                filters.Add(new FilePickerFileType("All Supported Files")
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

        private async void OnBrowseOutputClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Output Directory"
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
                        // call ViewModel's synchronous method (internally handles async)
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
                    var box = MessageBoxManager.GetMessageBoxStandard("Warning", "Output path is empty. Please set the output path first.");
                    await box.ShowAsync();
                    return;
                }

                var directory = Path.GetDirectoryName(vm.OutputPath);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("Warning", "Output directory does not exist.");
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
                _dragPressedArgs = e;  // Save event args
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
                // Use the saved _dragPressedArgs as the first parameter
                await DragDrop.DoDragDropAsync(_dragPressedArgs, dragData, DragDropEffects.Move);
                Debug.WriteLine("Drag finished");

                // Reset state
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

            //  External file drop
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

            // Internal row drag
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

        private static string T(string key, params object[] args)
        {
            var value = I18nManager.Instance.GetResource(key);
            if (string.IsNullOrEmpty(value))
                return key;
            return args.Length > 0 ? string.Format(value, args) : value;
        }
    }
}
