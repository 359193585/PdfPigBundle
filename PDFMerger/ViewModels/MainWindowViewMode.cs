// MainWindowViewMode.cs
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Lang.Avalonia;
using PdfMerger.Contracts;
using PdfMerger.Infrastructure;
using PdfMerger.Models;
using PdfMerger.Service;
using PdfMerger.Services;
using PdfSharp.Pdf.IO;
using SkiaSharp;

namespace PdfMerger.ViewModel
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly PdfSharpMergeService _pdfMergeService;
        public event EventHandler<string> ShowMessageRequested = delegate { };
        public static string DefaultOutputPdfName = "outputOfMerge.pdf";

        public MainWindowViewModel()
        {
            _pdfMergeService = new PdfSharpMergeService();
            InitCommands();
            FileItems.CollectionChanged += OnFileItemsChanged;
        }

        private void InitCommands()
        {
            AboutCommand = new RelayCommand(async () => await App.ShowAboutDialogAsync());
            CancelCommand = new RelayCommand(CancelMerge, () => IsMerging);
            ClearListCommand = new RelayCommand(ClearList);
            MergeCommand = new RelayCommand(async () => await MergePdfs(), () => FileItems.Count > 0 && !string.IsNullOrEmpty(OutputPath) && !IsMerging);
            MoveDownCommand = new RelayCommand(MoveDown, () => SelectedItem != null && FileItems.IndexOf(SelectedItem) < FileItems.Count - 1);
            MoveUpCommand = new RelayCommand(MoveUp, () => SelectedItem != null && FileItems.IndexOf(SelectedItem) > 0);
            RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => SelectedItem != null);
        }

        #region Properties for binding to the view

        private string _outputPath = string.Empty;
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (SetProperty(ref _outputPath, value))
                {
                    UpdateCanMerge();
                }
            }
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
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    UpdateMovementCommands();
                }
            }
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
        // output PDF document properties
        private bool _isSubjectManuallySet = false;
        private string _docTitle = "MergedFiles";
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
            set
            {
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

        // cancel support properties
        private CancellationTokenSource? _cts;

        private bool _isMerging;
        public bool IsMerging
        {
            get => _isMerging;
            set
            {
                if (SetProperty(ref _isMerging, value))
                {
                    UpdateCanMerge();
                }
            }
        }

        private void CancelMerge()
        {
            Debug.WriteLine($"---> [Cancel] 被调用了！时间: {DateTime.Now:HH:mm:ss.fff}");
            _cts?.Cancel();
            StatusMessage = T("Status_Cancelling", "Cancelling...");
        }
        public ICommand AboutCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;
        public ICommand ClearListCommand { get; private set; } = null!;
        public ICommand MergeCommand { get; private set; } = null!;
        public ICommand MoveDownCommand { get; private set; } = null!;
        public ICommand MoveUpCommand { get; private set; } = null!;
        public ICommand RemoveSelectedCommand { get; private set; } = null!;
        #endregion

        #region public methods for View to call (add files, set output path) 
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

            StatusMessage = T("Status_Loading");
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
                        // determine file type
                        string ext = Path.GetExtension(path).ToLower();
                        if (ext == ".pdf")
                            item.Type = FileType.Pdf;
                        else if (EnableImageSupport && (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif" || ext == ".tiff"))
                            item.Type = FileType.Image;
                        else
                            continue; // unsupported type, skip

                        // read information (PDF read page count, author; image can be ignored or read size, but temporarily keep simple information)
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
                                // image: set page count to 1, author to "Img"
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

                        // marshal the add operation to the UI thread via Dispatcher
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => FileItems.Add(item));
                    }
                }
            }).ContinueWith(_ =>
            {
                // when all files are processed, update status (also on UI thread)
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

        #region private methods (FileItemsChanged,clear, move up, move down, remove selected)
        private void OnFileItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCanMerge();
            UpdateMovementCommands();
        }
        // ---------- clear ----------
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

        // ---------- move up ----------
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

        // ---------- move down ----------
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

        // ---------- remove selected ----------
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
                // trigger message event
                var msg = T("Message_RemovedMissing", missingFiles.Count);
                ShowMessageRequested?.Invoke(this, msg);
                return true; // missing files found
            }
            return false; // no missing files
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
            if (_isSubjectManuallySet) return; // If the user has manually modified it, do not overwrite

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

        #region private core merging logic
        private async Task MergePdfs()
        {
            if (CheckAndCleanMissingFiles())
            {
                StatusMessage = T("Status_RemovedMissing");
                UpdateCanMerge();
                return;
            }

            if (FileItems.Count == 0 || string.IsNullOrEmpty(OutputPath)) return;

            if (CheckEncryptedFiles())
            {
                StatusMessage = T("Message_Move_Encrypted");
                return;
            }

            ResolveUniqueOutputPath();

            _cts = new CancellationTokenSource();
            IsMerging = true;
            CanMerge = false;
            StatusMessage = T("Status_MergePreparing");
            ProgressValue = 0;

            var filePaths = FileItems.Select(f => f.FilePath).ToArray();

            var progress = new Progress<MergeProgress>(p =>
            {
                if (_cts == null || _cts.IsCancellationRequested)
                {
                    return;
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_cts == null || _cts.IsCancellationRequested)
                    {
                        return;
                    }

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
                BookmarkGenerator = new SimpleBookmarkGenerator(),
                Title = DocTitle,
                Author = DocAuthor,
                Subject = DocSubject,
                Creator = DocCreator,
                AddPageNumbers = AddPageNumbers,
                CancellationToken = _cts.Token
            };

            try
            {
                var result = await _pdfMergeService.MergeAsync(filePaths, OutputPath, options, _cts.Token);
                _cts.Token.ThrowIfCancellationRequested();
                if (result != null)
                {
                    if (result.Success)
                    {
                        StatusMessage = T("Status_MergerSuccess", result.TotalPages, result.OutputPath ?? string.Empty);
                        if (result.DuplicatedFiles.Any())
                            StatusMessage += T("Status_IgnoreDuplicateFiles", result.DuplicatedFiles);
                    }
                    else
                    {
                        StatusMessage = T("Status_MergeFailed", result.ErrorMessage ?? string.Empty);
                    }
                }

            }
            catch (OperationCanceledException)
            {
                StatusMessage = T("Status_MergeCancelled");
                ProgressValue = 0;
            }
            catch (Exception ex)
            {
                StatusMessage = T("Status_MergeFailed", ex.Message);
            }
            finally
            {
                IsMerging = false;
                _cts?.Dispose();
                _cts = null;
                CanMerge = FileItems.Count > 0 && !string.IsNullOrEmpty(OutputPath);
            }
        }
        #endregion

        #region public methods for datagrid dragover and drop operate
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

        #region private helper methods
        private void UpdateCanMerge()
        {
            CanMerge = FileItems.Count > 0 && !string.IsNullOrEmpty(OutputPath) && !IsMerging; ;
            (MergeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        private void UpdateMovementCommands()
        {
            (MoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (MoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        private static string T(string key, params object[] args)
        {
            var value = I18nManager.Instance.GetResource(key);
            if (string.IsNullOrEmpty(value))
                return key;
            return args.Length > 0 ? string.Format(value, args) : value;
        }

        private void ResolveUniqueOutputPath()
        {
            if (!File.Exists(OutputPath)) return;

            string directory = Path.GetDirectoryName(OutputPath)!;
            string baseName = Path.GetFileNameWithoutExtension(OutputPath);
            string extension = Path.GetExtension(OutputPath);

            int counter = 1;
            string finalPath;
            do
            {
                finalPath = Path.Combine(directory, $"{baseName}_{counter}{extension}");
                counter++;
            } while (File.Exists(finalPath));

            OutputPath = finalPath;
        }

        
        #endregion
    }
}
