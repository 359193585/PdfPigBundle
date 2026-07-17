using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PdfPigBundle.Models
{
    public class FileItem : INotifyPropertyChanged
    {
        private string? _fileName;
        private string? _filePath;
        private int _pageCount;
        private long _fileSize;
        private string? _author;

        public string FilePath
        {
            get => _filePath ?? string.Empty;
            set => SetProperty(ref _filePath, value);
        }

        public string FileName
        {
            get => _fileName ?? string.Empty;
            set => SetProperty(ref _fileName, value);
        }

        public int PageCount
        {
            get => _pageCount;
            set => SetProperty(ref _pageCount, value);
        }

        public long FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        public string Author
        {
            get => _author ?? string.Empty;
            set => SetProperty(ref _author, value);
        }

        public string FileSizeDisplay => FileSize > 0 ? $"{FileSize / 1024.0:F1} KB" : "未知";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}