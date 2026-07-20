using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfPigBundle.Models;
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
