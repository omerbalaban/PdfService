using eLogo.PdfService.Models;
using eLogo.PdfService.Services.Domain.Collections.Interfaces;
using eLogo.PdfService.Services.Interfaces;
using eLogo.PdfService.Settings;
using HtmlAgilityPack;
using NAFCore.Common.Utils.Diagnostics.Logger;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services
{
    public class WkhtmlConverterService : IWkhtmlConvertService
    {
        private readonly AppSettings _appSettings;
        private readonly IImageResizer _imageResizer;
        private readonly ICompressService _compressService;
        private readonly INLogger _logger;
        private readonly IPdfTransactionCollection _pdfTransactionCollection;

        public WkhtmlConverterService(AppSettings appSettings, IImageResizer imageResizer, ICompressService compressService, IPdfTransactionCollection pdfTransactionCollection)
        {
            _logger = NLogger.Instance();
            _appSettings = appSettings;
            _imageResizer = imageResizer;
            _compressService = compressService;
            _pdfTransactionCollection = pdfTransactionCollection;
        }

        public Task<PdfResultBinary[]> ConvertHtmlToImage(HtmlToPdfModelBinary model)
        {
            throw new NotImplementedException();
        }

        public async Task<PdfResultBinary> ConvertHtmlToPdf(HtmlToPdfModelBinary model)
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

                await SavePdfTransactionRequest(model, PdfConverterType.WkHtmlToPDF, MethodInfo.GetCurrentMethod());

                htmlBuffer = model.Content;
                string requestId = await WriteTraceFile(model, htmlBuffer);

                string htmlContent = ClearHtmlDocument(Encoding.UTF8.GetString(htmlBuffer));

                using var restClient = new RestClient();
                restClient.AddDefaultHeader("Content-Type", "application/json; charset=utf-8");
                var restRequest = new RestRequest(_appSettings.wkhtmlPdfSettings.WkhtmlToPdfServiceUrl, Method.Post);

                string request = JsonConvert.SerializeObject(new HtmlToPdfModel
                {
                    Attachments = model.Attachments?.Select(s => new AttachmentModel { Data = Convert.ToBase64String(s.Data), FileName = s.FileName }).ToArray(),
                    Base64HtmlContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlContent)),
                    CorrelationId = model.CorrelationId,
                    CustomPropertyItems = model.CustomPropertyItems?.Select(s => new PropertyItem { Key = s.Key, Value = s.Value }).ToArray(),
                    DocumentTitle = string.IsNullOrEmpty(model.DocumentTitle) ? model.CorrelationId : model.DocumentTitle,
                    Margins = model.Margins,
                    PageOrientation = model.PageOrientation,
                    PageSize = model.PageSize,
                    PdfConverter = model.PdfConverter ?? 30,
                    Zoom = model.Zoom ?? 100,
                }, Formatting.Indented);

                restRequest.AddBody(request);

                var response = restClient.Execute<PdfResult>(restRequest);

                _logger.Info($"WkhtmlToPdf HtmlContent Temizleme Tamamlandi  {model.CorrelationId} DocTitle : {model.DocumentTitle}");

                // Nullify large objects to help GC
                htmlBuffer = null;
                model.Content = null;

                if (response.IsSuccessStatusCode)
                {
                    return new PdfResultBinary
                    {
                        Content = model.IsZipped ? _compressService.ZipData(response.Data.AsByteArray()) : response.Data.AsByteArray(),
                        Success = true,
                        Id = model.CorrelationId ?? Guid.NewGuid().ToString()
                    };
                }
                else
                    throw new ArgumentException($"Pdf Donusumu tamamlanamadi {model.CorrelationId} {model.DocumentTitle}, {response.ErrorMessage}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"{ex.Message}", model?.CorrelationId, model?.Content?.Length ?? 0);
                throw;
            }
            finally
            {
                htmlBuffer = null;
                if (model != null) model.Content = null;
            }
        }



        private string ClearHtmlDocument(string htmlInput)
        {
            using var ms = new MemoryStream();
            var document = new HtmlDocument();
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
