using eLogo.LogProvider.Interface;
using eLogo.PdfService.Models;
using eLogo.PdfService.Services.Interfaces;
using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services
{
    public abstract class BasePdfConverterService : IPdfConvertService
    {
        protected readonly IImageResizer _imageResizer;
        protected readonly IServiceLogger _logger;
        protected readonly ICompressService _compressService;

        protected BasePdfConverterService(IImageResizer imageResizer, IServiceLogger logger, ICompressService compressService)
        {
            _imageResizer = imageResizer;
            _logger = logger;
            _compressService = compressService;
        }

        public abstract Task<PdfResultBinary> ConvertHtmlToPdf(HtmlToPdfModelBinary model);

        // Template Method Pattern
        public async Task<PdfResultBinary[]> ConvertHtmlToImage(HtmlToPdfModelBinary model)
        {
            byte[] htmlBuffer = null;
            try
            {
                _logger.Info($"{GetType().Name} Convert Request Received {model.CorrelationId}", null, model.CorrelationId, model.Content.Length);

                if (model.IsZipped)
                    model.Content = _compressService.UnzipData(model.Content);

                htmlBuffer = model.Content;
                string htmlContent = ClearHtmlDocument(Encoding.UTF8.GetString(htmlBuffer));

                // Abstract method - each converter implements its own rendering logic
                var results = await RenderHtmlToImagesAsync(model, htmlContent);

                _logger.Info($"{GetType().Name} dönüşümü tamamlandı {model.CorrelationId}");

                // Nullify large objects to help GC
                htmlBuffer = null;
                model.Content = null;

                return results.Select(imageBytes => new PdfResultBinary
                {
                    Content = model.IsZipped ? _compressService.ZipData(imageBytes) : imageBytes,
                    Message = "OK",
                    Success = true,
                    Id = model.CorrelationId ?? Guid.NewGuid().ToString()
                }).ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error($"{GetType().Name} {ex.Message}", ex);
                throw;
            }
            finally
            {
                htmlBuffer = null;
                if (model != null) model.Content = null;
            }
        }

        // Abstract method for rendering - each converter implements differently
        protected abstract Task<byte[][]> RenderHtmlToImagesAsync(HtmlToPdfModelBinary model, string htmlContent);

        // Abstract method to get converter type
        protected abstract PdfConverterType GetConverterType();

        protected string ClearHtmlDocument(string htmlInput)
        {
            using var ms = new MemoryStream();
            var document = new HtmlDocument();
            document.OptionFixNestedTags = true;
            document.OptionAutoCloseOnEnd = true;
            document.LoadHtml(htmlInput);
            document.Save(ms, Encoding.UTF8);
            return RemoveConsoleLogs(Encoding.UTF8.GetString(ms.ToArray()).Replace("&#xA;", "").Replace("src=\".\"", "src=\"\""));
        }

        protected string RemoveConsoleLogs(string input)
        {
            string pattern = @"console\.log\(([^)]+)\);?";

            string cleanedHtml = Regex.Replace(input, pattern, string.Empty);

            if (cleanedHtml.Length > 1_250_000)
            {
                cleanedHtml = _imageResizer.CleanImages(cleanedHtml);
            }

            return cleanedHtml;
        }
              
    }
}
