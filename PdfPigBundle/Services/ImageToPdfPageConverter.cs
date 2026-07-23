using System;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfPigBundle.Services
{
    /// <summary>
    /// 图片转 PDF 页面转换器，支持多种输入源和页面尺寸模式。
    /// </summary>
    public class ImageToPdfPageConverter
    {
        /// <summary>
        /// 页面尺寸模式
        /// </summary>
        public enum PageSizeMode
        {
            /// <summary>页面大小自动适应图片尺寸</summary>
            FitImage,
            /// <summary>固定 A4 大小（595×842 点），图片居中缩放以适应</summary>
            A4,
            /// <summary>自定义大小（需指定宽度和高度）</summary>
            Custom
        }

        /// <summary>
        /// 默认页面尺寸模式
        /// </summary>
        public PageSizeMode DefaultMode { get; set; } = PageSizeMode.FitImage;

        /// <summary>
        /// 默认自定义宽度（点），当 DefaultMode = Custom 时使用
        /// </summary>
        public double? DefaultCustomWidth { get; set; }

        /// <summary>
        /// 默认自定义高度（点），当 DefaultMode = Custom 时使用
        /// </summary>
        public double? DefaultCustomHeight { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="defaultMode">默认页面尺寸模式</param>
        /// <param name="defaultCustomWidth">自定义宽度（点），mode=Custom 时必填</param>
        /// <param name="defaultCustomHeight">自定义高度（点），mode=Custom 时必填</param>
        public ImageToPdfPageConverter(
            PageSizeMode defaultMode = PageSizeMode.FitImage,
            double? defaultCustomWidth = null,
            double? defaultCustomHeight = null)
        {
            DefaultMode = defaultMode;
            DefaultCustomWidth = defaultCustomWidth;
            DefaultCustomHeight = defaultCustomHeight;
        }

        // ---------- 从文件路径转换 ----------

        /// <summary>
        /// 从图片文件转换为 PDF 文档（使用默认设置）
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(string imagePath)
            => ConvertImageToPdfDocument(imagePath, DefaultMode, DefaultCustomWidth, DefaultCustomHeight);

        /// <summary>
        /// 从图片文件转换为 PDF 文档（指定尺寸模式）
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(string imagePath, PageSizeMode mode, double? customWidth = null, double? customHeight = null)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("图片文件不存在", imagePath);

            using (var stream = File.OpenRead(imagePath))
                return ConvertImageToPdfDocument(stream, mode, customWidth, customHeight);
        }

        // ---------- 从字节数组转换 ----------

        /// <summary>
        /// 从图片字节数组转换为 PDF 文档（使用默认设置）
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(byte[] imageData)
            => ConvertImageToPdfDocument(imageData, DefaultMode, DefaultCustomWidth, DefaultCustomHeight);

        /// <summary>
        /// 从图片字节数组转换为 PDF 文档（指定尺寸模式）
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(byte[] imageData, PageSizeMode mode, double? customWidth = null, double? customHeight = null)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("图片数据不能为空", nameof(imageData));

            using (var ms = new MemoryStream(imageData))
                return ConvertImageToPdfDocument(ms, mode, customWidth, customHeight);
        }

        // ---------- 从流转换（核心实现） ----------

        /// <summary>
        /// 从图片流转换为 PDF 文档（使用默认设置）
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(Stream imageStream)
            => ConvertImageToPdfDocument(imageStream, DefaultMode, DefaultCustomWidth, DefaultCustomHeight);

        /// <summary>
        /// 从图片流转换为 PDF 文档（指定尺寸模式）—— 核心方法
        /// </summary>
        public PdfDocument ConvertImageToPdfDocument(Stream imageStream, PageSizeMode mode, double? customWidth = null, double? customHeight = null)
        {
            if (imageStream == null || !imageStream.CanRead)
                throw new ArgumentException("无效的图片流", nameof(imageStream));

            // 确定页面尺寸
            double pageWidth, pageHeight;
            switch (mode)
            {
                case PageSizeMode.FitImage:
                    // 我们使用 XImage.FromStream 直接读取，但注意流可能被用完。
                    using (var tempImage = XImage.FromStream(imageStream))
                    {
                        pageWidth = tempImage.PointWidth;
                        pageHeight = tempImage.PointHeight;
                    }
                    // 但此时流已经读完，后续绘制需要重新读取图片。重新定位流
                    if (imageStream.CanSeek)
                        imageStream.Seek(0, SeekOrigin.Begin);
                    else
                        throw new InvalidOperationException("流不可重置，无法多次读取。请使用字节数组或可重置流。");
                    break;
                case PageSizeMode.A4:
                    pageWidth = 595;
                    pageHeight = 842;
                    break;
                case PageSizeMode.Custom:
                    if (!customWidth.HasValue || !customHeight.HasValue)
                        throw new ArgumentException("Custom 模式必须指定 customWidth 和 customHeight");
                    pageWidth = customWidth.Value;
                    pageHeight = customHeight.Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), "不支持的页面尺寸模式");
            }

            // 创建文档和页面
            var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = XUnit.FromPoint(pageWidth);
            page.Height = XUnit.FromPoint(pageHeight);

            // 绘制图片（如果模式是 FitImage，我们已经读取过，重新定位流后再次读取）
            using (var gfx = XGraphics.FromPdfPage(page))
            {
                // 若模式是 FitImage，我们需要再读取一次图片以绘制；否则只需读取一次。
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

        // ---------- 简便方法：直接添加页面到现有文档 ----------

        /// <summary>
        /// 将图片转换为一页，并直接添加到指定的 PdfDocument（不返回新文档）
        /// </summary>
        public void AddImagePageToDocument(string imagePath, PdfDocument targetDoc, PageSizeMode mode = PageSizeMode.FitImage, double? customWidth = null, double? customHeight = null)
        {
            if (targetDoc == null)
                throw new ArgumentNullException(nameof(targetDoc));

            using (var tempDoc = ConvertImageToPdfDocument(imagePath, mode, customWidth, customHeight))
            {
                // 导入 tempDoc 的页面到 targetDoc（使用 PdfReader.Open 以 Import 模式）
                // 由于我们已经有了 tempDoc，但 PdfDocument 不能直接导入，需要保存为流再打开
                // 更简单：直接遍历页面并添加到 targetDoc（但 page 属于 tempDoc，不能直接 AddPage）
                // 正确做法：使用 PdfReader.Open 以 Import 模式打开 tempDoc 保存的流
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
