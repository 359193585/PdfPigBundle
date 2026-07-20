// MainWindowViewMode.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using PdfPigBundle.Models;
using PdfPigBundle.Service;
using UglyToad.PdfPig;


namespace PdfPigBundle.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        //private readonly MergePdfFiles _merger = new MergePdfFiles();
        private readonly PdfSharpMergeService _merger = new PdfSharpMergeService();
        public event EventHandler<string> ShowMessageRequested;

        public ObservableCollection<FileItem> FileItems { get; } = new ObservableCollection<FileItem>();

        private string _outputPath;
        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
        }

        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private string _statusMessage = "就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _canMerge;
        public bool CanMerge
        {
            get => _canMerge;
            set => SetProperty(ref _canMerge, value);
        }

        private FileItem _selectedItem;
        public FileItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public ICommand ClearListCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand RemoveSelectedCommand { get; }
        public ICommand AboutCommand { get; }

        public ICommand MergeCommand { get; }

        public  static string DefaultOutputPdfName = "outputOfMerge.pdf";
        public MainWindowViewModel()
        {
            ClearListCommand = new RelayCommand(ClearList);
            MoveUpCommand = new RelayCommand(MoveUp, () => SelectedItem != null && FileItems.IndexOf(SelectedItem) > 0);
            MoveDownCommand = new RelayCommand(MoveDown, () => SelectedItem != null && FileItems.IndexOf(SelectedItem) < FileItems.Count - 1);
            RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => SelectedItem != null);

            AboutCommand = new RelayCommand(async () => await App.ShowAboutDialogAsync());

            MergeCommand = new RelayCommand(async () => await MergePdfs(), () => FileItems.Count > 0 && !string.IsNullOrEmpty(OutputPath));

            FileItems.CollectionChanged += (s, e) =>
            {
                UpdateCanMerge();
                UpdateMovementCommands();
            };

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(OutputPath))
                    UpdateCanMerge();
                else if (e.PropertyName == nameof(SelectedItem))
                    UpdateMovementCommands();
            };
        }

        private void UpdateCanMerge()
        {
            CanMerge = FileItems.Count > 0 && !string.IsNullOrEmpty(OutputPath);
            (MergeCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        private void UpdateMovementCommands()
        {
            (MoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (MoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #region  公开方法供 View 调用（添加文件、设置输出路径） 
        public void AddFiles(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;

            // 如果输出路径为空，自动设置为第一个文件所在目录
            if (string.IsNullOrEmpty(OutputPath) && paths.Length > 0)
            {
                var dir = Path.GetDirectoryName(paths[0]);
                if (!string.IsNullOrEmpty(dir))
                    OutputPath = Path.Combine(dir, DefaultOutputPdfName);
            }

            StatusMessage = "正在读取文件信息...";
            ProgressValue = 0;

            // 注意：因为此方法在 UI 线程被调用（来自 Click 或 Drop 事件），
            // 所以我们在这里同步读取 PDF 信息，但为了避免阻塞 UI，使用 Task.Run 将耗时操作放到后台。
            // 但更新集合必须在 UI 线程完成。
            Task.Run(() =>
            {
                foreach (var path in paths)
                {
                    if (File.Exists(path) && !FileItems.Any(f => f.FilePath == path))
                    {
                        var item = new FileItem { FilePath = path, FileName = Path.GetFileName(path) };
                        try
                        {
                            using (var doc = PdfDocument.Open(path))
                            {
                                item.PageCount = doc.NumberOfPages;
                                item.Author = doc.Information?.Author ?? "";
                            }
                            var fi = new FileInfo(path);
                            item.FileSize = fi.Length;
                        }
                        catch
                        {
                            item.PageCount = 0;
                            item.Author = "读取失败";
                            item.FileSize = 0;
                        }

                        // 通过 Dispatcher 将添加操作封送到 UI 线程
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => FileItems.Add(item));
                    }
                }
            }).ContinueWith(_ =>
            {
                // 当所有文件处理完成后，更新状态（也在 UI 线程）
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    StatusMessage = $"已加载 {FileItems.Count} 个文件";
                    UpdateCanMerge();
                });
            });
        }

        public void SetOutputPath(string path)
        {
            OutputPath = path;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

        #region 私有方法（清空、上移、下移、删除选中、合并）
        // ---------- 清空 ----------
        private void ClearList()
        {
            FileItems.Clear();
            OutputPath = "";
            StatusMessage = "列表已清空";
            UpdateCanMerge();
        }

        // ---------- 上移 ----------
        private void MoveUp()
        {
            if (SelectedItem == null) return;
            int index = FileItems.IndexOf(SelectedItem);
            if (index > 0)
            {
                var item = FileItems[index];
                FileItems.RemoveAt(index);      // 移除
                FileItems.Insert(index - 1, item); // 插入到前一个位置
                SelectedItem = item;            // 保持选中
                UpdateMovementCommands();       // 刷新按钮状态（可选）
            }
        }

        // ---------- 下移 ----------
        private void MoveDown()
        {
            if (SelectedItem == null) return;
            int index = FileItems.IndexOf(SelectedItem);
            if (index < FileItems.Count - 1)
            {
                var item = FileItems[index];
                FileItems.RemoveAt(index);
                FileItems.Insert(index + 1, item);
                SelectedItem = item;
                UpdateMovementCommands();
            }
        }

        // ---------- 删除选中 ----------
        private void RemoveSelected()
        {
            if (SelectedItem != null)
            {
                FileItems.Remove(SelectedItem);
                SelectedItem = null;
                UpdateCanMerge();
                (MoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (MoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        private bool CheckAndCleanMissingFiles()
        {
            var missingFiles = FileItems.Where(f => !File.Exists(f.FilePath)).ToList();
            if (missingFiles.Any())
            {
                foreach (var item in missingFiles)
                {
                    FileItems.Remove(item);
                }
                // 触发消息事件
                ShowMessageRequested?.Invoke(this, $"已自动移除 {missingFiles.Count} 个不存在的文件。请确认列表后重新合并。");
                return true; // 有缺失
            }
            return false; // 无缺失
        }
        // ---------- 合并 ----------
        private async Task MergePdfs()
        {
            if (CheckAndCleanMissingFiles())
            {
                StatusMessage = "已清理缺失文件，请检查列表。";
                // 更新按钮状态
                UpdateCanMerge();
                return;
            }

            if (FileItems.Count ==0 || string.IsNullOrEmpty(OutputPath)) return;

            // ---- 处理输出文件已存在的情况 生成带序号的新路径----
            string originalPath = OutputPath;          // 保存用户指定的原始路径
            string finalPath = originalPath;

            if (File.Exists(finalPath))
            {
                string directory = Path.GetDirectoryName(originalPath)!;
                string baseName = Path.GetFileNameWithoutExtension(originalPath); // 原始文件名（不含扩展名）
                string extension = Path.GetExtension(originalPath);

                int counter = 1;
                do
                {
                    string newName = $"{baseName}_{counter}{extension}";
                    finalPath = Path.Combine(directory, newName);
                    counter++;
                } while (File.Exists(finalPath));

                // 更新 OutputPath 属性，界面同步显示新路径
                OutputPath = finalPath;
            }

            var filePaths = FileItems.Select(f => f.FilePath).ToArray();

            CanMerge = false;
            StatusMessage = "准备合并...";
            ProgressValue = 0;

            try
            {
                var progress = new Progress<MergeProgress>(p =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (p.IsComplete)
                        {
                            StatusMessage = $"✅ 合并完成！共 {p.TotalPagesProcessed} 页";
                            ProgressValue = 100;
                        }
                        else
                        {
                            StatusMessage = $"📄 正在合并 [{p.FileIndex + 1}/{p.TotalFiles}]: {p.FileName} ({p.PageCount} 页)";
                            ProgressValue = p.PercentComplete;
                        }
                    });
                });

                //var result = await Task.Run(() =>
                //    _merger.Merge(filePaths, OutputPath, ignoreDuplicates: false, progress)
                //);

                var options = new MergeOptions
                {
                    IgnoreDuplicates = false,
                    Progress = progress,
                    BookmarkGenerator = new SimpleBookmarkGenerator() // 实现 IBookmarkGenerator 接口的类，用于生成书签
                };
                var result = await Task.Run(() => _merger.Merge(filePaths, OutputPath, options));


                if (result.Success)
                {
                    StatusMessage = $"✅ 成功！总页数：{result.TotalPages}，文件：{result.OutputPath}";
                    if (result.DuplicatedFiles.Any())
                        StatusMessage += $"\n⚠️ 忽略重复文件：{string.Join(", ", result.DuplicatedFiles)}";
                }
                else
                {
                    StatusMessage = $"❌ 合并失败：{result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"💥 发生异常：{ex.Message}";
            }
            finally
            {
                CanMerge = FileItems.Count > 0 && !string.IsNullOrEmpty(OutputPath);
            }
        }
        #endregion

      
    }
}
