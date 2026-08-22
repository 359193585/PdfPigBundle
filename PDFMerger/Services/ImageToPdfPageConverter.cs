//ImageToPdfPageConverter.cs
using System;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PdfMerger.Services
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
            FitImage,
            A4,
            Custom
        }

        public PageSizeMode DefaultMode { get; set; } = PageSizeMode.FitImage;
        public double? DefaultCustomWidth { get; set; }
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

        // ---------- From File Path ----------

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
                throw new FileNotFoundException("Image file not found", imagePath);
            var doc = new PdfDocument();
            AddImagePageToDocument(imagePath, doc, mode, customWidth, customHeight);
            return doc;
        }

        // ---------- From Byte Array ----------

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
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));

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
            var doc = new PdfDocument();
            AddImageStreamToDocument(imageStream, doc, mode, customWidth, customHeight);
            return doc;
        }

        // ---------- simple method: directly add a page to an existing document ----------

        /// <summary>
        /// Directly converts an image file and draws it onto a new page in the target PdfDocument without temporary document serializations.
        /// </summary>
        public void AddImagePageToDocument(
            string imagePath,
            PdfDocument targetDoc,
            PageSizeMode mode = PageSizeMode.FitImage,
            double? customWidth = null,
            double? customHeight = null)
        {
            if (targetDoc == null)
                throw new ArgumentNullException(nameof(targetDoc));

            try
            {
                // Get the image stream and add it to the document on read mode. This is the standard approach for most image formats.
                using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                AddImageStreamToDocument(stream, targetDoc, mode, customWidth, customHeight);
            }
            catch (Exception)
            {
                // for macOS and non-standard images, Use ImageSharp for cross-platform image cleaning (Fix EXIF/CMYK/Corrupted Headers)
                using var cleanedStream = CleanAndNormalizeImage(imagePath);
                if (cleanedStream != null)
                {
                    AddImageStreamToDocument(cleanedStream, targetDoc, mode, customWidth, customHeight);
                }
                else
                {
                    throw new InvalidOperationException($"Failed to process or normalize image file on current OS: {imagePath}");
                }
            }
        }

        /// <summary>
        /// Core drawing logic: Draws an image stream directly into a target PdfDocument.
        /// </summary>
        public void AddImageStreamToDocument(
            Stream imageStream,
            PdfDocument targetDoc,
            PageSizeMode mode,
            double? customWidth = null,
            double? customHeight = null)
        {
            if (imageStream == null || !imageStream.CanRead)
                throw new ArgumentException("Invalid image stream", nameof(imageStream));

            if (targetDoc == null)
                throw new ArgumentNullException(nameof(targetDoc));

            using var xImage = XImage.FromStream(imageStream);
            double pageWidth, pageHeight;
            switch (mode)
            {
                case PageSizeMode.FitImage:
                    pageWidth = xImage.PointWidth;
                    pageHeight = xImage.PointHeight;
                    break;

                case PageSizeMode.A4:
                    pageWidth = 595.0;  // Standard A4 width in points
                    pageHeight = 842.0; // Standard A4 height in points
                    break;

                case PageSizeMode.Custom:
                    if (!customWidth.HasValue || !customHeight.HasValue)
                        throw new ArgumentException("Custom mode requires specifying customWidth and customHeight");
                    pageWidth = customWidth.Value;
                    pageHeight = customHeight.Value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), "Unsupported page size mode");
            }

            PdfPage page = targetDoc.AddPage();
            page.Width = XUnit.FromPoint(pageWidth);
            page.Height = XUnit.FromPoint(pageHeight);

            using (XGraphics gfx = XGraphics.FromPdfPage(page))
            {
                double scaleX = pageWidth / xImage.PointWidth;
                double scaleY = pageHeight / xImage.PointHeight;
                double scale = Math.Min(scaleX, scaleY);

                double drawWidth = xImage.PointWidth * scale;
                double drawHeight = xImage.PointHeight * scale;
                double x = (pageWidth - drawWidth) / 2.0;
                double y = (pageHeight - drawHeight) / 2.0;

                gfx.DrawImage(xImage, x, y, drawWidth, drawHeight);
            }
        }
        private const int MAX_IMAGE_DIMENSION = 2560;
        /// <summary>
        /// used for macOS and non-standard images to ensure cross-platform compatibility.
        /// </summary>
        private MemoryStream? CleanAndNormalizeImage(string imagePath)
        {
            try
            {
                using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath);
                // Automatically handle EXIF orientation for images taken by iPhone/Mac to prevent upside-down rendering
                image.Mutate(x => x.AutoOrient());

                if (image.Width > MAX_IMAGE_DIMENSION || image.Height > MAX_IMAGE_DIMENSION)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new SixLabors.ImageSharp.Size(MAX_IMAGE_DIMENSION, MAX_IMAGE_DIMENSION)
                    }));
                }

                var ms = new MemoryStream();
                image.Save(ms, new PngEncoder());
                ms.Position = 0;
                return ms;
            }
            catch
            {
                return null;
            }
        }
    }
}
