//FileItem.cs
using PdfPigBundle.Infrastructure;

namespace PdfPigBundle.Models
{
    public class FileItem : ObservableObject
    {

        private string? _filePath;
        public string FilePath
        {
            get => _filePath ?? string.Empty;
            set => SetProperty(ref _filePath, value);
        }

        private string? _fileName;
        public string FileName
        {
            get => _fileName ?? string.Empty;
            set => SetProperty(ref _fileName, value);
        }

        private int _pageCount;
        public int PageCount
        {
            get => _pageCount;
            set => SetProperty(ref _pageCount, value);
        }

        private long _fileSize;
        public long FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }
        public string FileSizeDisplay => FileSize > 0 ? $"{FileSize / 1024.0:F1} KB" : "未知";

        private string? _author;
        public string Author
        {
            get => _author ?? string.Empty;
            set => SetProperty(ref _author, value);
        }

        private FileType _type = FileType.Pdf;
        public FileType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public bool IsImage => Type == FileType.Image;
       

    }
    public enum FileType
    {
        Pdf,
        Image
    }
}
