using eLogo.PdfService.Models;
using eLogo.PdfService.Services.Domain.Collections.Interfaces;
using eLogo.PdfService.Services.Interfaces;
using eLogo.PdfService.Settings;
using HtmlAgilityPack;
using IronSoftware.Drawing;
using NAFCore.Common.Utils.Diagnostics.Logger;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services
{
    public class IronPdfConverterService : IIronPdfConverter
    {
        private readonly INLogger _logger;
        private readonly AppSettings _appSettings;
        private readonly IImageResizer _imageResizer;
        private readonly ICompressService _compressService;
        private readonly IPdfTransactionCollection _pdfTransactionCollection;

        public IronPdfConverterService(AppSettings appSettings, IImageResizer imageResizer, ICompressService compressService, IPdfTransactionCollection pdfTransactionCollection)
        {
            _appSettings = appSettings;
            _imageResizer = imageResizer;
            _compressService = compressService;
            _pdfTransactionCollection = pdfTransactionCollection;
            _logger = NLogger.Instance();

            _logger.Info($"Iron PDF License : {IronPdf.License.IsLicensed}");
        }

        private IronPdf.ChromePdfRenderer CreateRenderer(HtmlToPdfModelBinary model)
        {
            var renderer = new IronPdf.ChromePdfRenderer();
            renderer.RenderingOptions.EnableJavaScript = true;
            renderer.RenderingOptions.RenderDelay = 50; //ms
            renderer.RenderingOptions.Timeout = _appSettings.RenderTimeout;
            renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Screen;
            renderer.RenderingOptions.Zoom = _appSettings.ZoomFactor == 0 ? 95 : _appSettings.ZoomFactor;
            renderer.RenderingOptions.CreatePdfFormsFromHtml = false;
            renderer.RenderingOptions.PrintHtmlBackgrounds = true;
            renderer.RenderingOptions.PaperSize = GetPageSize(model.PageSize);
            renderer.RenderingOptions.FitToPaperMode = IronPdf.Engines.Chrome.FitToPaperModes.Zoom;
            renderer.RenderingOptions.Zoom = model.Zoom ?? 90;
            renderer.RenderingOptions.PaperOrientation = GetOrientation(model.PageOrientation);
            renderer.RenderingOptions.Title = $"{(!string.IsNullOrEmpty(model.DocumentTitle) ? model.DocumentTitle : Guid.NewGuid().ToString())}.pdf";
            renderer.RenderingOptions.MarginBottom = model.Margins;
            renderer.RenderingOptions.MarginRight = model.Margins;
            renderer.RenderingOptions.MarginLeft = model.Margins;
            renderer.RenderingOptions.MarginTop = model.Margins;
            return renderer;
        }

        public async Task<PdfResultBinary> ConvertHtmlToPdf(HtmlToPdfModelBinary model)
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

                await SavePdfTransactionRequest(model, PdfConverterType.IronPdfConverter, MethodInfo.GetCurrentMethod());

                htmlBuffer = model.Content;

                string requestId = await WriteTraceFile(model, htmlBuffer);

                renderer = CreateRenderer(model);

                string htmlContent = ClearHtmlDocument(Encoding.UTF8.GetString(htmlBuffer));

                var renderTask = renderer.RenderHtmlAsPdfAsync(htmlContent);

                var timeoutTask = Task.Delay(_appSettings.RenderTaskTimeout * 1000); //convert seconds to milliseconds
                if (await Task.WhenAny(renderTask, timeoutTask) == timeoutTask)
                {
                    throw new TimeoutException($"PDF rendering exceeded timeout of {_appSettings.RenderTaskTimeout} seconds");
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

                if (_appSettings.HtmlTraceEnable)
                    _ = Task.Run(() => File.Delete(Path.Combine(_appSettings.HtmlPath, $"{requestId}.html")));

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
                _logger.Error(ex, $"Timeout: {ex.Message}", model?.CorrelationId, model?.Content?.Length ?? 0);
                throw;
            }
            finally
            {
                htmlBuffer = null;
                if (model != null) model.Content = null;
                renderer = null;
            }
        }


        public async Task<PdfResultBinary[]> ConvertHtmlToImage(HtmlToPdfModelBinary model)
        {
            byte[] htmlBuffer = null;
            try
            {
                _logger.Info($"IronPDF Convert Request Received {model.CorrelationId}", null, model.CorrelationId, model.Content.Length);
                if (model.IsZipped)
                    model.Content = _compressService.UnzipData(model.Content);

                htmlBuffer = model.Content;

                await SavePdfTransactionRequest(model, PdfConverterType.IronPdfConverter, MethodInfo.GetCurrentMethod());

                string requestId = await WriteTraceFile(model, htmlBuffer);

                string htmlContent = ClearHtmlDocument(Encoding.UTF8.GetString(htmlBuffer));

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

                _logger.Info($"PDF dönüşümü tamamlandı {model.CorrelationId} DocTitle : {model.DocumentTitle} {pdfDocument.BinaryData.Length}");

                var images = pdfDocument.ToBitmap(96);

                if (_appSettings.HtmlTraceEnable)
                    File.Delete(Path.Combine(_appSettings.HtmlPath, $"{model.CorrelationId.Replace("|", "_")}.html"));

                // Nullify large objects to help GC
                htmlBuffer = null;
                model.Content = null;

                return images.Select(s => new PdfResultBinary
                {
                    Content = model.IsZipped ? _compressService.ZipData(ConvertToImage(s)) : ConvertToImage(s),
                    Message = "OK",
                    Success = true,
                    Id = model.CorrelationId ?? Guid.NewGuid().ToString()
                }).ToArray();

            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"IronPDF {ex.Message}", model?.CorrelationId, model?.Content?.Length ?? 0);
                throw;
            }
            finally
            {
                htmlBuffer = null;
                if (model != null) model.Content = null;
            }
        }

        private byte[] ConvertToImage(AnyBitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.ExportStream(ms, AnyBitmap.ImageFormat.Png);
            return ms.ToArray();
        }

        private string ClearHtmlDocument(string htmlInput)
        {
            using var ms = new MemoryStream();
            HtmlDocument document = new HtmlDocument();
            document.OptionFixNestedTags = true;
            document.OptionAutoCloseOnEnd = true;
            document.LoadHtml(htmlInput);
            document.Save(ms, Encoding.UTF8);
            return RemoveConsoleLogs(Encoding.UTF8.GetString(ms.ToArray()).Replace("&#xA;", "").Replace("src=\".\"", "src=\"\""));
        }

        private string RemoveConsoleLogs(string input)
        {
            string pattern = @"console\.log\(([^)]+)\);?";

            string cleanedHtml = Regex.Replace(input, pattern, string.Empty);

            if (cleanedHtml.Length > 1_250_000)
            {
                cleanedHtml = this._imageResizer.CleanImages(cleanedHtml);
            }

            return cleanedHtml;
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

        private async Task<string> WriteTraceFile(HtmlToPdfModelBinary model, byte[] htmlBuffer)
        {
            string requestId = $"{model.DocumentTitle}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss_fff}";
            if (_appSettings.HtmlTraceEnable)
            {
                await File.WriteAllBytesAsync(Path.Combine(_appSettings.HtmlPath, $"{requestId}.html"), htmlBuffer);
            }

            return requestId;
        }


        private async Task SavePdfTransactionRequest(HtmlToPdfModelBinary model, PdfConverterType pdfConverter, MethodBase method)
        {
            if (model.Content.Length > _appSettings.RequestLimit * 1024)
                throw new InvalidDataException($"HTML Boyutu izin verilen limitlerin üzerinde {_appSettings.RequestLimit} KB");

            if (!_appSettings.TransactionLogCounterEnable)
                return;

            var sourceId = model.CustomPropertyItems?.FirstOrDefault(s => s.Key == "SourceId")?.Value;
            var vkn = model.CustomPropertyItems?.FirstOrDefault(s => s.Key == "VKN")?.Value;
            var userAccounRef = model.CustomPropertyItems?.FirstOrDefault(s => s.Key == "UserAccountRef")?.Value;

            await _pdfTransactionCollection.InsertAsync(new Services.Domain.Models.PdfApiTransaction()
            {
                ApplicationName = _appSettings.ApplicationName,
                ClientKey = string.Empty,
                CorrelationId = model.CorrelationId,
                CreatedAt = DateTime.Now,
                DocumentTitle = model.DocumentTitle,
                PageOrientation = model.PageOrientation,
                PdfConverter = (int)(model.PdfConverter ?? (int)PdfConverterType.Default),
                RequestSize = model.Content.Length,
                Source = string.IsNullOrEmpty(sourceId) ? "LEGACY" : sourceId,
                Vkn = string.IsNullOrEmpty(vkn) ? "" : vkn,
                EndPoint = method.Name,
                UserAccounRef = userAccounRef
            });
        }
    }
}
