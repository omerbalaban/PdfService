using eLogo.LogProvider.Interface;
using eLogo.PdfService.Models;
using eLogo.PdfService.Services.Interfaces;
using IronSoftware.Drawing;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services
{
    public class IronPdfConverterService : BasePdfConverterService
    {
        public IronPdfConverterService(IServiceLogger logger, IImageResizer imageResizer, ICompressService compressService) : base(imageResizer, logger, compressService)
        {
        }

        protected override PdfConverterType GetConverterType() => PdfConverterType.IronPdfConverter;

        private IronPdf.ChromePdfRenderer CreateRenderer(HtmlToPdfModelBinary model)
        {
            var renderer = new IronPdf.ChromePdfRenderer();
            renderer.RenderingOptions.EnableJavaScript = Settings.Settings.AppSetting.EnableJavaScript;
            //renderer.RenderingOptions.RenderDelay = 50; //ms
            renderer.RenderingOptions.Timeout = Settings.Settings.AppSetting.RenderTimeout;
            renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print;
            renderer.RenderingOptions.Zoom = Settings.Settings.AppSetting.ZoomFactor == 0 ? 95 : Settings.Settings.AppSetting.ZoomFactor;
            //renderer.RenderingOptions.Zoom = model.Zoom ?? 90;

            renderer.RenderingOptions.CreatePdfFormsFromHtml = false;
            renderer.RenderingOptions.PrintHtmlBackgrounds = true;
            renderer.RenderingOptions.PaperSize = GetPageSize(model.PageSize);
            renderer.RenderingOptions.FitToPaperMode = IronPdf.Engines.Chrome.FitToPaperModes.Zoom;
            renderer.RenderingOptions.PaperOrientation = GetOrientation(model.PageOrientation);
            renderer.RenderingOptions.Title = $"{(!string.IsNullOrEmpty(model.DocumentTitle) ? model.DocumentTitle : Guid.NewGuid().ToString())}.pdf";
            renderer.RenderingOptions.MarginBottom = model.Margins;
            renderer.RenderingOptions.MarginRight = model.Margins;
            renderer.RenderingOptions.MarginLeft = model.Margins;
            renderer.RenderingOptions.MarginTop = model.Margins;
            return renderer;
        }

        public override async Task<PdfResultBinary> ConvertHtmlToPdf(HtmlToPdfModelBinary model)
        {
            byte[] htmlBuffer = null;
            IronPdf.ChromePdfRenderer renderer = null;
            try
            {
                _logger.Info($"IronPDF Convert Request Received {model.CorrelationId} DocTitle : {model.DocumentTitle}", null, model.CorrelationId, model.Content.Length);

                if (model.IsZipped)
                {
                    model.Content = _compressService.UnzipData(model.Content);

                    if (model.Attachments != null && model.Attachments.Length > 0)
                        foreach (var attachment in model.Attachments)
                            attachment.Data = _compressService.UnzipData(attachment.Data);
                }

                if (model.Content.Length > Settings.Settings.AppSetting.LargePdfLimitAsByte)
                    throw new InvalidDataException($"HTML Boyutu izin verilen limitlerin üzerinde. Limit: {Settings.Settings.AppSetting.LargePdfLimitAsByte} bytes, PdfSize: {model.Content.Length} bytes");

                htmlBuffer = model.Content;
                renderer = CreateRenderer(model);

                string htmlContent = ClearHtmlDocument(Encoding.UTF8.GetString(htmlBuffer));

                var renderTask = renderer.RenderHtmlAsPdfAsync(htmlContent);

                var timeoutTask = Task.Delay(Settings.Settings.AppSetting.RenderTaskTimeout * 1000); //convert seconds to milliseconds
                if (await Task.WhenAny(renderTask, timeoutTask) == timeoutTask)
                {
                    throw new TimeoutException($"PDF rendering exceeded timeout of {Settings.Settings.AppSetting.RenderTaskTimeout} seconds");
                }

                var pdfDocument = await renderTask;

                if (pdfDocument.BinaryData == null || pdfDocument.BinaryData.Length == 0)
                    throw new InvalidOperationException($" PDF Render islemi hatalı, null result {model.CorrelationId} DocTitle : {model.DocumentTitle}");


                if (model.Attachments != null && model.Attachments.Length > 0)
                {
                    foreach (var s in model.Attachments)
                        pdfDocument.Attachments.AddAttachment(s.FileName, s.Data);

                    _logger.Info($"Iron PDF Attachment ekleme tamamlandı {model.CorrelationId} DocTitle : {model.DocumentTitle} {pdfDocument.BinaryData.Length}");
                }

                if (model.CustomPropertyItems != null && model.CustomPropertyItems.Length > 0)
                {
                    foreach (var s in model.CustomPropertyItems)
                        pdfDocument.MetaData.CustomProperties.Add(s.Key, s.Value);
                }

                _logger.Info($"Iron PDF dönüşümü tamamlandı {model.CorrelationId} DocTitle : {model.DocumentTitle} {pdfDocument.BinaryData.Length}");

                htmlBuffer = null;
                model.Content = null;

                return new PdfResultBinary
                {
                    Content = model.IsZipped ? _compressService.ZipData(pdfDocument.Stream.ToArray()) : pdfDocument.Stream.ToArray(),
                    Success = true,
                    Id = model.CorrelationId ?? Guid.NewGuid().ToString()
                };

            }
            catch (TimeoutException ex)
            {
                _logger.Error($"Timeout: {ex.Message}", ex);
                throw;
            }
            finally
            {
                htmlBuffer = null;
                if (model != null) model.Content = null;
                renderer = null;
            }
        }

        protected override async Task<byte[][]> RenderHtmlToImagesAsync(HtmlToPdfModelBinary model, string htmlContent)
        {
            var renderer = new IronPdf.ChromePdfRenderer();
            renderer.RenderingOptions.PrintHtmlBackgrounds = true;
            renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Screen;
            renderer.RenderingOptions.EnableJavaScript = true;
            renderer.RenderingOptions.Zoom = model.Zoom ?? 90;
            renderer.RenderingOptions.PaperOrientation = GetOrientation(model.PageOrientation);
            renderer.RenderingOptions.Title = $"{(!string.IsNullOrEmpty(model.DocumentTitle) ? model.DocumentTitle : Guid.NewGuid().ToString())}.pdf";
            renderer.RenderingOptions.MarginBottom = model.Margins;
            renderer.RenderingOptions.MarginRight = model.Margins;
            renderer.RenderingOptions.MarginLeft = model.Margins;
            renderer.RenderingOptions.MarginTop = model.Margins;

            var pdfDocument = await renderer.RenderHtmlAsPdfAsync(htmlContent);
            if (pdfDocument.BinaryData == null || pdfDocument.BinaryData.Length == 0)
                throw new InvalidOperationException($" PDF Render islemi hatalı, null result {model.CorrelationId} DocTitle : {model.DocumentTitle}");

            var images = pdfDocument.ToBitmap(96);

            return images.Select(ConvertToImage).ToArray();
        }

        private byte[] ConvertToImage(AnyBitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.ExportStream(ms, AnyBitmap.ImageFormat.Png);
            return ms.ToArray();
        }

        private TEnum ParseEnumOrDefault<TEnum>(string value, TEnum defaultValue) where TEnum : struct, Enum
        {
            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, true, out var result))
                return result;
            return defaultValue;
        }

        private IronPdf.Rendering.PdfPaperSize GetPageSize(string pageSize)
        {
            return ParseEnumOrDefault(pageSize, IronPdf.Rendering.PdfPaperSize.A4);
        }

        private IronPdf.Rendering.PdfPaperOrientation GetOrientation(string orientation)
        {
            return ParseEnumOrDefault(orientation, IronPdf.Rendering.PdfPaperOrientation.Portrait);
        }
    }
}
