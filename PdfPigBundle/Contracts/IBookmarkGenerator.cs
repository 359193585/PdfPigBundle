//IBookmarkGenerator.cs

using System.Collections.Generic;

namespace PdfPigBundle.Contracts;

/// <summary>
/// 书签生成器，用于为合并后的文档生成书签（目录）。
/// </summary>
public interface IBookmarkGenerator
        {
            /// <summary>
            /// 根据每个源文件的起始页码和元信息生成书签列表。
            /// </summary>
            /// <param name="fileEntries">每个源文件的起始页码和文件名</param>
            /// <returns>书签列表，每个书签包含标题和跳转页码（从1开始）</returns>
            IList<BookmarkEntry> GenerateBookmarks(IList<FileBookmarkInfo> fileEntries);
        }

/// <summary>
/// 源文件的书签信息
/// </summary>
public class FileBookmarkInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileNameWithoutExtension { get; set; } = string.Empty;
    public int StartPageNumber { get; set; } // 该文件在最终文档中的起始页码（1-based）
    public int PageCount { get; set; }
}

/// <summary>
/// 书签条目
/// </summary>
public class BookmarkEntry
{
    public string Title { get; set; } = string.Empty;
    public int PageNumber { get; set; } // 目标页码（1-based）
}
    

