//IBookmarkGenerator.cs

using System.Collections.Generic;

namespace PdfPigBundle.Contracts;

/// <summary>
/// 书签生成器，用于为合并后的文档生成书签（目录）。
/// Bookmark generator, used to generate bookmarks (table of contents) for merged documents.
/// </summary>
public interface IBookmarkGenerator
        {
            /// <summary>
            /// 根据每个源文件的起始页码和元信息生成书签列表。
            /// Generate a list of bookmarks based on the starting page number and metadata of each source file.
            /// </summary>
            /// <param name="fileEntries">每个源文件的起始页码和文件名</param>
            /// <returns>书签列表，每个书签包含标题和跳转页码（从1开始）</returns>
            IList<BookmarkEntry> GenerateBookmarks(IList<FileBookmarkInfo> fileEntries);
        }

/// <summary>
/// 源文件的书签信息
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
/// 书签条目
/// Bookmark entry
/// </summary>
public class BookmarkEntry
{
    public string Title { get; set; } = string.Empty;
    public int PageNumber { get; set; } // The target page number (1-based)
}
    

