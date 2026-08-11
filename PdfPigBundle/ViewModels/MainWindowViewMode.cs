// MainWindowViewMode.cs
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Lang.Avalonia;
using PdfPigBundle.Contracts;
using PdfPigBundle.Infrastructure;
using PdfPigBundle.Models;
using PdfPigBundle.Service;
using PdfPigBundle.Services;
using PdfSharp.Pdf.IO;

namespace PdfPigBundle.ViewModel
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly PdfSharpMergeService _merger = new PdfSharpMergeService();
        public event EventHandler<string> ShowMessageRequested = delegate { };

       
        private string _outputPath = string.Empty;
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

        private string _statusMessage = T("Status_Ready");
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

        private FileItem? _selectedItem = null!;
        public FileItem? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private bool _enableAddDuplicateCheck = true;
        public bool EnableAddDuplicateCheck
        {
            get => _enableAddDuplicateCheck;
            set => SetProperty(ref _enableAddDuplicateCheck, value);
        }

        private bool _enableImageSupport = false;
        public bool EnableImageSupport
        {
            get => _enableImageSupport;
            set => SetProperty(ref _enableImageSupport, value);
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

            // if OutputPath is empty, set it to the directory of the first file with a default name
            if (string.IsNullOrEmpty(OutputPath) && paths.Length > 0)
            {
                var dir = Path.GetDirectoryName(paths[0]);
                if (!string.IsNullOrEmpty(dir))
                    OutputPath = Path.Combine(dir, DefaultOutputPdfName);
            }

            StatusMessage =T("Status_Loading");
            ProgressValue = 0;

            // Note: This method is called on the UI thread (from Click or Drop events),
            // so we read PDF information synchronously here, but to avoid blocking the UI, we use Task.Run to perform time-consuming operations in the background.
            // However, updating the collection must be done on the UI thread.
            Task.Run(() =>
            {
                foreach (var path in paths)
                {
                    if (File.Exists(path) && (EnableAddDuplicateCheck ? !FileItems.Any(f => f.FilePath == path) : true))
                    {
                        var item = new FileItem { FilePath = path, FileName = Path.GetFileName(path) };
                        // 判断文件类型
                        string ext = Path.GetExtension(path).ToLower();
                        if (ext == ".pdf")
                            item.Type = FileType.Pdf;
                        else if (EnableImageSupport && (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif" || ext == ".tiff"))
                            item.Type = FileType.Image;
                        else
                            continue; // 不支持的类型，跳过

                        // 读取信息（PDF 读取页数、作者；图片可以不读或读尺寸，但暂时保留简单信息）
                        try
                        {
                            if (item.Type == FileType.Pdf) // pdf file
                            {
                                try
                                {
                                    using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import))
                                    {
                                        item.PageCount = doc.PageCount;
                                        item.Author = doc.Info.Author ?? "";
                                    }
                                }
                                catch (PdfReaderException ex)
                                {
                                    if (ex.Message.Contains("password") || ex.Message.Contains("encrypted"))
                                    {
                                        item.IsEncrypted = true;
                                        item.Author = T("Status_Encrypted");
                                        item.PageCount = 0;
                                    }
                                    else
                                    {
                                        throw; // other errors, rethrow to be caught by outer catch
                                    }
                                }
                            }
                            else  // image file
                            {
                                // 图片：页数设为1，作者设为"图片"
                                item.PageCount = 1;
                                item.Author = "Img";
                            }
                            var fi = new FileInfo(path);
                            item.FileSize = fi.Length;
                        }
                        catch
                        {
                            item.PageCount = 0;
                            item.Author = "ReadError";
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
                    UpdateCanMerge();
                    UpdateDefaultSubject();
                    StatusMessage = FileItems.Count > 0
                     ? T("Status_ListLoaded", FileItems.Count)
                     : T("Status_ListEmpty");
                });
            });
        }

        public void SetOutputPath(string path)
        {
            OutputPath = path;
        }
      
        #endregion

        #region 私有方法（清空、上移、下移、删除选中、合并）
        // ---------- 清空 ----------
        private void ClearList()
        {
            FileItems.Clear();
            OutputPath = "";
            UpdateCanMerge();
            UpdateDefaultSubject();
            StatusMessage = FileItems.Count > 0
                          ? T("Status_ListLoaded", FileItems.Count)
                          : T("Status_ListEmpty");

        }

        // ---------- 上移 ----------
        private void MoveUp()
        {
            if (SelectedItem == null) return;
            int index = FileItems.IndexOf(SelectedItem);
            if (index > 0)
            {
                var item = FileItems[index];
                FileItems.RemoveAt(index);      
                FileItems.Insert(index - 1, item); 
                SelectedItem = item;            
                UpdateMovementCommands();       
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

                StatusMessage = FileItems.Count > 0
                     ? T("Status_ListLoaded", FileItems.Count)
                     : T("Status_ListEmpty");
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
                var msg = T("Message_RemovedMissing", missingFiles.Count);
                ShowMessageRequested?.Invoke(this, msg);
                return true; // 有缺失
            }
            return false; // 无缺失
        }
        // ---------- 合并 ----------
        private async Task MergePdfs()
        {
            if (CheckAndCleanMissingFiles())
            {
                StatusMessage = T("Status_RemovedMissing");
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
            StatusMessage = T("Status_MergePreparing");
            ProgressValue = 0;

            try
            {
                var progress = new Progress<MergeProgress>(p =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (p.IsComplete)
                        {
                            StatusMessage = T("Status_MergeComplete", p.TotalPagesProcessed);
                            ProgressValue = 100;
                        }
                        else
                        {
                            StatusMessage = T("Status_MergeProgress",
                                               p.FileIndex + 1,
                                               p.TotalFiles,
                                               p.FileName ?? string.Empty,
                                               p.PageCount);
                            ProgressValue = p.PercentComplete;
                        }
                    });
                });

                var options = new MergeOptions
                {
                    IgnoreDuplicates = false,
                    Progress = progress,
                    BookmarkGenerator = new SimpleBookmarkGenerator(), // 实现 IBookmarkGenerator 接口的类，用于生成书签
                    Title = DocTitle,
                    Author = DocAuthor,
                    Subject = DocSubject,
                    Creator = DocCreator,
                    AddPageNumbers = AddPageNumbers
                };

                if (CheckEncryptedFiles())
                {
                    StatusMessage = T("Message_Move_Encrypted");
                    return;
                }

                var result =  await Task.Run(() => _merger.Merge(filePaths, OutputPath, options));
                try
                {
                    if (result != null)
                    {

                        if (result.Success)
                        {
                            StatusMessage = T("Status_MergerSuccess", result.TotalPages, result.OutputPath ?? string.Empty);
                            if (result.DuplicatedFiles.Any())
                                StatusMessage += $"\n⚠️ 忽略重复文件：{string.Join(", ", result.DuplicatedFiles)}";
                        }
                        else
                        {
                            StatusMessage = T("Status_MergeFailed", result.ErrorMessage ?? string.Empty);
                        }
                    }
                }
                catch { }

              
            }
            catch (Exception ex)
            {
                StatusMessage = T("Status_MergeFailed", ex.Message);
            }
            finally
            {
                CanMerge = FileItems.Count > 0 && !string.IsNullOrEmpty(OutputPath);
            }
        }

        private bool CheckEncryptedFiles()
        {
            var encrypted = FileItems.Where(f => f.IsEncrypted).ToList();
            if (encrypted.Any())
            {
                var msg = T("Message_EncryptedFiles", string.Join(", ", encrypted.Select(f => f.FileName)));
                ShowMessageRequested?.Invoke(this, msg);
                return true;
            }
            return false;
        }
        private void UpdateDefaultSubject()
        {
            if (_isSubjectManuallySet) return; // 如果用户已手动修改，不覆盖

            if (FileItems.Count == 0)
            {
                _docSubject = "";
                OnPropertyChanged(nameof(DocSubject));
                return;
            }

            string firstFileName = Path.GetFileNameWithoutExtension(FileItems[0].FileName);
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string subject = $"{date} {firstFileName} MergeredFiles";
            _docSubject = subject;
            OnPropertyChanged(nameof(DocSubject));
        }
        #endregion

        #region  PDF 文档属性
        private bool _isSubjectManuallySet = false;
        private string _docTitle = "MergeredFiles";
        public string DocTitle
        {
            get => _docTitle;
            set => SetProperty(ref _docTitle, value);
        }

        private string _docAuthor = "User of PDFMerger";
        public string DocAuthor
        {
            get => _docAuthor;
            set => SetProperty(ref _docAuthor, value);
        }

        private string _docSubject = "";
        public string DocSubject
        {
            get => _docSubject;
            set {
                if (SetProperty(ref _docSubject, value))
                    _isSubjectManuallySet = true;
            }
        }

        private string _docCreator = "PDFMerger";
        public string DocCreator
        {
            get => _docCreator;
            set => SetProperty(ref _docCreator, value);
        }

        private bool _addPageNumbers;
        public bool AddPageNumbers
        {
            get => _addPageNumbers;
            set => SetProperty(ref _addPageNumbers, value);
        }
        #endregion

        #region datagrid dragover and drop operate
        public ObservableCollection<FileItem> FileItems { get; } = new ObservableCollection<FileItem>();
        public void MoveFileItem(FileItem dragged, FileItem target)
        {
            int oldIndex = FileItems.IndexOf(dragged);
            int newIndex = FileItems.IndexOf(target);
            Debug.WriteLine($"MoveFileItem: oldIndex={oldIndex}, newIndex={newIndex}");
            if (oldIndex != newIndex && oldIndex >= 0 && newIndex >= 0)
            {
                int adjustedNewIndex = (oldIndex < newIndex) ? newIndex - 1 : newIndex;
                FileItems.RemoveAt(oldIndex);
                FileItems.Insert(adjustedNewIndex, dragged);
                SelectedItem = FileItems[adjustedNewIndex];

                var names = string.Join(", ", FileItems.Select(x => x.FileName));
                Debug.WriteLine($"After Move: {names}");

            }
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
