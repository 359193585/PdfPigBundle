using System;
using System.IO;
using System.Reflection;
using Avalonia.Platform;
using PdfSharp.Fonts;

public class CustomFontResolver : IFontResolver
{
    private static string localFontFilename = "NotoSans-SemiBold.ttf";
    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        // 这里简化处理：对于 Helvetica 请求，始终返回同一个字体文件
        if (familyName == "Helvetica")
        {
            // 返回字体信息，faceName 是唯一标识，用于后续获取字体数据
            return new FontResolverInfo("NotoSans");
        }
        return null;
    }

    public byte[]? GetFont(string faceName)
    {
      
        // 根据 ResolveTypeface 返回的 faceName 读取字体文件字节
        if (faceName == "NotoSans")
        {
            try
            {
                var uri = new Uri("avares://PDFMerger/Assets/NotoSans-SemiBold.ttf");
                using var stream = AssetLoader.Open(uri);
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return bytes;
            }
            catch
            {
                string fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", localFontFilename);
                if (File.Exists(fontPath))
                    return File.ReadAllBytes(fontPath);
            }
        }
        return null;
    }
}
