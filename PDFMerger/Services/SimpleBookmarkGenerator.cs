
//SimpleBookmarkGenerator.cs
using System.Collections.Generic;
using System.Linq;
using PdfMerger.Contracts;

namespace PdfMerger.Services;
/// <summary>
/// Class implementing the IBookmarkGenerator interface, used for generating bookmarks
/// </summary>
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
