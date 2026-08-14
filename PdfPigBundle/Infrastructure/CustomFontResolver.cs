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
        // here simplify processing: for Helvetica requests, always return the same font file
        if (familyName == "Helvetica")
        {
            return new FontResolverInfo("NotoSans");
        }
        return null;
    }

    public byte[]? GetFont(string faceName)
    {
      
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
