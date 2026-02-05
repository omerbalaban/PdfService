using eLogo.LogProvider.Interface;
using eLogo.LogProvider.LogService;
using eLogo.PdfService.Models;
using eLogo.PdfService.Services.Domain.Models;
using eLogo.PdfService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace eLogo.PdfService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Consumes("application/x-msgpack", "application/json")]
    [Produces("application/x-msgpack", "application/json")]
    public class ConvertController : ControllerBase
    {
        private readonly IServiceLogger _logger;
        private readonly Dictionary<PdfConverterType, IPdfConvertService> _converterList;
        private readonly ITransactionTrackingService _trackingService;


        public ConvertController(IServiceLogger logger, IPdfConvertService ironPdfService, IPdfConvertService wkhtmlConvertService, ITransactionTrackingService trackingService)
        {
            _converterList = new Dictionary<PdfConverterType, IPdfConvertService>
            {
                { PdfConverterType.Default, ironPdfService },
                { PdfConverterType.IronPdfConverter, ironPdfService },
                { PdfConverterType.WkHtmlToPDF, wkhtmlConvertService }
            };

            _logger = logger;
            _trackingService = trackingService;
        }

        [HttpPost("HtmlToPdf")]
        [RequestSizeLimit((long)7 * 1024 * 1024)]
        public async Task<ActionResult<PdfResultBinary>> ConvertHtmlToPdf([FromBody] HtmlToPdfModelBinary requestModel)
        {
            try
            {
                if (requestModel == null || requestModel.Content == null)
                    return BadRequest(new PdfResultBinary { Success = false, Message = "Request data can not be null" });

                var converterService = SelectPdfConverterService(requestModel, out var pdfConverterType);

                TrackRequest(requestModel, pdfConverterType, "HtmlToPdf");

                PdfResultBinary result = await converterService.ConvertHtmlToPdf(requestModel);

                requestModel.Content = null;

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error("Html to pdf conversion operation failed.", ex);
                return BadRequest(new PdfResultBinary { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.Error("Html to pdf conversion operation failed.", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, new PdfResultBinary { Success = false, Message = ex.Message });
            }
            finally
            {
                if (requestModel != null)
                    requestModel.Content = null;
            }
        }

        [HttpPost("HtmlToImage")]
        [RequestSizeLimit((long)7 * 1024 * 1024)]
        public async Task<ActionResult<PdfResultBinary[]>> ConvertHtmlToImage([FromBody] HtmlToPdfModelBinary requestModel)
        {
            try
            {
                if (requestModel == null || requestModel.Content == null)
                    return BadRequest(new PdfResultBinary[] { new PdfResultBinary { Success = false, Message = "Request data can not be null" } });

                var converterService = SelectPdfConverterService(requestModel, out var pdfConverterType);

                TrackRequest(requestModel, pdfConverterType, "HtmlToImage");

                PdfResultBinary[] result = await converterService.ConvertHtmlToImage(requestModel);

                requestModel.Content = null;

                return Ok(result); // ASP.NET Core will serialize as MessagePack

            }
            catch (InvalidOperationException ex)
            {
                _logger.Error("Html to pdf conversion operation failed.", ex);
                return BadRequest(new PdfResultBinary { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.Error("Html to pdf conversion operation failed.", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, new PdfResultBinary { Success = false, Message = ex.Message });
            }
            finally
            {
                if (requestModel != null)
                    requestModel.Content = null;
            }
        }


        private IPdfConvertService SelectPdfConverterService(HtmlToPdfModelBinary model, out PdfConverterType pdfConverterType)
        {
            if (model == null || model.Content == null)
                throw new InvalidOperationException("Model or Content data can not be null");

            pdfConverterType = model.PdfConverter.HasValue ? (PdfConverterType)model.PdfConverter.Value : PdfConverterType.Default;
            if (!_converterList.TryGetValue(pdfConverterType, out IPdfConvertService converterService))
            {
                converterService = _converterList[PdfConverterType.Default];
            }

            if (model.Content.Length > Settings.Settings.AppSetting.OversizePdfLimit)
            {
                _logger.Info($"Pdf Belge Boyutu izin verilenin üzerinde. (Size Of Document: {model.Content.Length}), Convertor override ediliyor. {PdfConverterType.WkHtmlToPDF}", null, model.CorrelationId);
                converterService = _converterList[PdfConverterType.WkHtmlToPDF];
            }

            return converterService;
        }

        private void TrackRequest(HtmlToPdfModelBinary model, PdfConverterType converterType, string endpoint)
        {
            if (!Settings.Settings.AppSetting.TransactionCounterLogEnable)
                return;

            try
            {
                var sourceId = model.CustomPropertyItems?.FirstOrDefault(s => s.Key == "SourceId")?.Value;
                var userAccounRef = model.CustomPropertyItems?.FirstOrDefault(s => s.Key == "UserAccountRef")?.Value;

                var transaction = new TransactionTrackingModel
                {
                    ApplicationName = Settings.Settings.AppSetting.ApplicationName,
                    CorrelationId = model.CorrelationId,
                    CreatedAt = DateTime.Now,
                    DocumentTitle = model.DocumentTitle,
                    PageOrientation = model.PageOrientation,
                    PdfConverter = (int)(model.PdfConverter ?? (int)converterType),
                    RequestSize = model.Content?.Length ?? 0,
                    Source = string.IsNullOrEmpty(sourceId) ? "LEGACY" : sourceId,
                    EndPoint = endpoint,
                    IpAddress = IpAddressResolver.GetClientIpAddress(),
                    UserAccounRef = userAccounRef
                };

                _trackingService.TrackRequestFireAndForget(transaction);

            }
            catch (Exception ex)
            {
                _logger.Error("Failed to track conversion request", ex);
            }
        }


    }
}