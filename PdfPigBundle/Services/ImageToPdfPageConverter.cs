//ImageToPdfPageConverter.cs
using System;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace PdfPigBundle.Services
{
    /// <summary>
    /// image to pdf converter, supports multiple input sources and page size modes.
    /// </summary>
    public class ImageToPdfPageConverter
    {
        /// <summary>
        /// page size mode
        /// </summary>
        public enum PageSizeMode
        {
            /// <summary>page size automatically fits the image size</summary>
            FitImage,
            /// <summary>fixed A4 size (595×842 points), image is centered and scaled to fit</summary>
            A4,
            /// <summary>custom size (requires specifying width and height)</summary>
            Custom
        }

        /// <summary>
        /// default page size mode
        /// </summary>
        public PageSizeMode DefaultMode { get; set; } = PageSizeMode.FitImage;

        /// <summary>
        /// default custom width (points), used when DefaultMode = Custom
        /// </summary>
        public double? DefaultCustomWidth { get; set; }

        /// <summary>
        /// default custom height (points), used when DefaultMode = Custom
        /// </summary>
        public double? DefaultCustomHeight { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="defaultMode">default page size mode</param>
        /// <param name="defaultCustomWidth">default custom width (points), required when mode=Custom</param>
        /// <param name="defaultCustomHeight">default custom height (points), required when mode=Custom</param>
        public ImageToPdfPageConverter(
            PageSizeMode defaultMode = PageSizeMode.FitImage,
            double? defaultCustomWidth = null,
            double? defaultCustomHeight = null)
        {
            DefaultMode = defaultMode;
            DefaultCustomWidth = defaultCustomWidth;
            DefaultCustomHeight = defaultCustomHeight;
        }

        // ---------- from file path ----------

        /// <summary>
        /// converts an image file to a PDF document (using default settings)
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(string imagePath)
            => ConvertImageToPdfDocument(imagePath, DefaultMode, DefaultCustomWidth, DefaultCustomHeight);

        /// <summary>
        /// converts an image file to a PDF document (specified page size mode)
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(string imagePath, PageSizeMode mode, double? customWidth = null, double? customHeight = null)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("image file not found", imagePath);

            bool isJpg = imagePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                imagePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
            try
            {

                using (var stream = File.OpenRead(imagePath))
                    return ConvertImageToPdfDocument(stream, mode, customWidth, customHeight);
            }
            catch (Exception) when (isJpg) 
            {
                using (var ms = new MemoryStream())
                {
                    using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath))
                    {
                        image.Save(ms, new PngEncoder());
                        ms.Position = 0;
                        return ConvertImageToPdfDocument(ms, mode, customWidth, customHeight);
                    }
                }
            }
        }

        // ---------- from byte array ----------

        /// <summary>
        /// converts an image byte array to a PDF document (using default settings)
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(byte[] imageData)
            => ConvertImageToPdfDocument(imageData, DefaultMode, DefaultCustomWidth, DefaultCustomHeight);

        /// <summary>
        /// converts an image byte array to a PDF document (specified page size mode)
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(byte[] imageData, PageSizeMode mode, double? customWidth = null, double? customHeight = null)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("image data cannot be null or empty", nameof(imageData));

            using (var ms = new MemoryStream(imageData))
                return ConvertImageToPdfDocument(ms, mode, customWidth, customHeight);
        }

        // ---------- from stream (core implementation) ----------

        /// <summary>
        /// converts an image stream to a PDF document (using default settings)
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(Stream imageStream)
            => ConvertImageToPdfDocument(imageStream, DefaultMode, DefaultCustomWidth, DefaultCustomHeight);

        /// <summary>
        /// converts an image stream to a PDF document (specified page size mode) — core method
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(Stream imageStream, PageSizeMode mode, double? customWidth = null, double? customHeight = null)
        {
            if (imageStream == null || !imageStream.CanRead)
                throw new ArgumentException("invalid image stream", nameof(imageStream));

            // determine page size
            double pageWidth, pageHeight;
            switch (mode)
            {
                case PageSizeMode.FitImage:
                    // use XImage.FromStream to read directly, but be aware the stream may be consumed.
                    using (var tempImage = XImage.FromStream(imageStream))
                    {
                        pageWidth = tempImage.PointWidth;
                        pageHeight = tempImage.PointHeight;
                    }
                    // but at this point the stream has been read, subsequent drawing requires re-reading the image. reposition the stream
                    if (imageStream.CanSeek)
                        imageStream.Seek(0, SeekOrigin.Begin);
                    else
                        throw new InvalidOperationException("stream cannot be reset, cannot read multiple times. please use a byte array or a resettable stream.");
                    break;
                case PageSizeMode.A4:
                    pageWidth = 595;
                    pageHeight = 842;
                    break;
                case PageSizeMode.Custom:
                    if (!customWidth.HasValue || !customHeight.HasValue)
                        throw new ArgumentException("Custom mode requires specifying customWidth and customHeight");
                    pageWidth = customWidth.Value;
                    pageHeight = customHeight.Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), "unsupported page size mode");
            }

            // create document and page
            var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = XUnit.FromPoint(pageWidth);
            page.Height = XUnit.FromPoint(pageHeight);

            // draw image (if mode is FitImage, we have already read it, reposition the stream and read again)
            using (var gfx = XGraphics.FromPdfPage(page))
            {
                // if mode is FitImage, we need to read the image again to draw; otherwise, only need to read once.
                using (var image = XImage.FromStream(imageStream))
                {
                    double scaleX = pageWidth / image.PointWidth;
                    double scaleY = pageHeight / image.PointHeight;
                    double scale = Math.Min(scaleX, scaleY);

                    double drawWidth = image.PointWidth * scale;
                    double drawHeight = image.PointHeight * scale;
                    double x = (pageWidth - drawWidth) / 2;
                    double y = (pageHeight - drawHeight) / 2;

                    gfx.DrawImage(image, x, y, drawWidth, drawHeight);
                }
            }

            return doc;
        }

        // ---------- simple method: directly add a page to an existing document ----------

        /// <summary>
        /// converts an image to a page and directly adds it to the specified PdfDocument (does not return a new document)
        /// </summary>
        public void AddImagePageToDocument(string imagePath, PdfDocument targetDoc, PageSizeMode mode = PageSizeMode.FitImage, double? customWidth = null, double? customHeight = null)
        {
            if (targetDoc == null)
                throw new ArgumentNullException(nameof(targetDoc));

            using (var tempDoc = ConvertImageToPdfDocument(imagePath, mode, customWidth, customHeight))
            {
                using (var ms = new MemoryStream())
                {
                    tempDoc.Save(ms);
                    ms.Position = 0;
                    using (var importDoc = PdfReader.Open(ms, PdfDocumentOpenMode.Import))
                    {
                        foreach (PdfPage page in importDoc.Pages)
                            targetDoc.AddPage(page);
                    }
                }
            }
        }
    }
}
