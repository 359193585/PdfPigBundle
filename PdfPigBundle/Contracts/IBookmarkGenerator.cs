//IBookmarkGenerator.cs

using System.Collections.Generic;

namespace PdfPigBundle.Contracts;

/// <summary>
/// Bookmark generator, used to generate bookmarks (table of contents) for merged documents.
/// </summary>
public interface IBookmarkGenerator
        {
            /// <summary>
            /// Generate a list of bookmarks based on the starting page number and metadata of each source file.
            /// </summary>
            /// <param name="fileEntries">source file bookmark information</param>
            /// <returns>a list of bookmarks, each containing a title and a target page number (1-based)</returns>
            IList<BookmarkEntry> GenerateBookmarks(IList<FileBookmarkInfo> fileEntries);
        }

/// <summary>
/// Source file bookmark information
/// </summary>
public class FileBookmarkInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileNameWithoutExtension { get; set; } = string.Empty;
    public int StartPageNumber { get; set; } // The starting page number of the file in the final document (1-based)
    public int PageCount { get; set; }
}

/// <summary>
/// Bookmark entry
/// </summary>
public class BookmarkEntry
{
    public string Title { get; set; } = string.Empty;
    public int PageNumber { get; set; } // The target page number (1-based)
}
    

