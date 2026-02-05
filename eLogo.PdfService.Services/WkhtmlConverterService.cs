using DinkToPdf;
using DinkToPdf.Contracts;
using eLogo.LogProvider.Interface;
using eLogo.PdfService.Models;
using eLogo.PdfService.Services.Interfaces;
using System;
using System.Text;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services
{
    public class WkhtmlConverterService : BasePdfConverterService
    {
        private readonly IConverter _dinkToPdfConverter;

        public WkhtmlConverterService(IServiceLogger logger, IImageResizer imageResizer, ICompressService compressService, IConverter dinkToPdfConverter) : base(imageResizer, logger, compressService)
        {
            _dinkToPdfConverter = dinkToPdfConverter;
        }

        protected override PdfConverterType GetConverterType() => PdfConverterType.WkHtmlToPDF;

        protected override Task<byte[][]> RenderHtmlToImagesAsync(HtmlToPdfModelBinary model, string htmlContent)
        {
            throw new NotImplementedException("WkhtmlToPdf does not support HTML to Image conversion");
        }

        public override async Task<PdfResultBinary> ConvertHtmlToPdf(HtmlToPdfModelBinary model)
        {
            byte[] htmlBuffer = null;
            try
            {
                _logger.Info($"WkhtmlToPdf Convert Request Received {model.CorrelationId} DocTitle : {model.DocumentTitle}", null, model.CorrelationId, model.Content.Length);

                if (model.IsZipped)
                {
                    model.Content = _compressService.UnzipData(model.Content);

                    if (model.Attachments != null && model.Attachments.Length > 0)
                        foreach (var attachment in model.Attachments)
                            attachment.Data = _compressService.UnzipData(attachment.Data);
                }

                htmlBuffer = model.Content;
                string htmlContent = ClearHtmlDocument(Encoding.UTF8.GetString(htmlBuffer));

                _logger.Info($"WkhtmlToPdf HtmlContent Temizleme Tamamlandi {model.CorrelationId} DocTitle : {model.DocumentTitle}");

                // DinkToPdf conversion
                var globalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = GetOrientation(model.PageOrientation),
                    PaperSize = GetPaperSize(model.PageSize),
                    Margins = new MarginSettings { Top = model.Margins, Bottom = model.Margins, Left = model.Margins, Right = model.Margins },
                    DocumentTitle = string.IsNullOrEmpty(model.DocumentTitle) ? model.CorrelationId : model.DocumentTitle
                };

                var objectSettings = new ObjectSettings
                {
                    PagesCount = true,
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" },
                    HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = false },
                    FooterSettings = { FontSize = 9, Line = false, Center = model.DocumentTitle }
                };

                var document = new HtmlToPdfDocument()
                {
                    GlobalSettings = globalSettings,
                    Objects = { objectSettings }
                };

                byte[] pdfBytes = _dinkToPdfConverter.Convert(document);

                // Nullify large objects to help GC
                htmlBuffer = null;
                model.Content = null;

                _logger.Info($"WkhtmlToPdf Conversion Completed {model.CorrelationId} DocTitle : {model.DocumentTitle}");

                return new PdfResultBinary
                {
                    Content = model.IsZipped ? _compressService.ZipData(pdfBytes) : pdfBytes,
                    Success = true,
                    Id = model.CorrelationId ?? Guid.NewGuid().ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"WkhtmlToPdf conversion failed: {ex.Message}", ex);
                throw;
            }
            finally
            {
                htmlBuffer = null;
                if (model != null) model.Content = null;
            }
        }

        private Orientation GetOrientation(string orientation)
        {
            if (string.IsNullOrEmpty(orientation))
                return Orientation.Portrait;

            return orientation.ToLower() switch
            {
                "landscape" => Orientation.Landscape,
                "portrait" => Orientation.Portrait,
                _ => Orientation.Portrait
            };
        }

        private PaperKind GetPaperSize(string pageSize)
        {
            if (string.IsNullOrEmpty(pageSize))
                return PaperKind.A4;

            return pageSize.ToUpper() switch
            {
                "A3" => PaperKind.A3,
                "A4" => PaperKind.A4,
                "A5" => PaperKind.A5,
                "LETTER" => PaperKind.Letter,
                "LEGAL" => PaperKind.Legal,
                _ => PaperKind.A4
            };
        }
    }
}
