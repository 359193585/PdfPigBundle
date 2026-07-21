
//SimpleBookmarkGenerator.cs
using System.Collections.Generic;
using System.Linq;
using PdfPigBundle.Contracts;

namespace PdfPigBundle.Services;
public class SimpleBookmarkGenerator : IBookmarkGenerator
{
    public IList<BookmarkEntry> GenerateBookmarks(IList<FileBookmarkInfo> fileEntries)
    {
        return fileEntries.Select(f => new BookmarkEntry
        {
            Title = f.FileNameWithoutExtension,
            PageNumber = f.StartPageNumber
        }).ToList();
    }
}
