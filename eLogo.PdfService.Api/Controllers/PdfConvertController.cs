using eLogo.PdfService.Models;
using eLogo.PdfService.Services.Interfaces;
using eLogo.PdfService.Settings;
using Microsoft.AspNetCore.Mvc;
using NAFCore.Common.Utils.Diagnostics.Logger;
using NAFCore.Platform.Services.Hosting.APIDoc.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace eLogo.PdfService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Consumes("application/x-msgpack", "application/json")]
    [Produces("application/x-msgpack", "application/json")]
    public class PdfConvertController : ControllerBase
    {
        private readonly INLogger _logger;
        private readonly Dictionary<PdfConverterType, IPdfConvertService> _converterList;
        private readonly AppSettings _appSettings;


        public PdfConvertController(AppSettings appSettings, IIronPdfConverter ironPdfService, IWkhtmlConvertService wkhtmlConvertService)
        {
            _converterList = new Dictionary<PdfConverterType, IPdfConvertService>
            {
                { PdfConverterType.Default, ironPdfService },
                { PdfConverterType.IronPdfConverter, ironPdfService },
                { PdfConverterType.WkHtmlToPDF, wkhtmlConvertService }
            };

            _appSettings = appSettings;
            _logger = NLogger.Instance();
        }

        [HttpPost("HtmlToPdf")]
        [SwaggerOperation(OperationId = "HtmlToPdf")]
        [SwaggerGroup("BinaryConversion")]
        [RequestSizeLimit((long)7 * 1024 * 1024)]
        public async Task<ActionResult<PdfResultBinary>> ConvertHtmlToPdf([FromBody] HtmlToPdfModelBinary requestModel)
        {
            try
            {
                if (requestModel == null || requestModel.Content == null)
                    return BadRequest(new PdfResultBinary { Success = false, Message = "Request data can not be null" });

                var converterService = SelectPdfConverterService(requestModel, out var pdfConverterType);

                PdfResultBinary result = await converterService.ConvertHtmlToPdf(requestModel);

                requestModel.Content = null;

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ex, "Html to pdf conversion operation failed.", requestModel?.CorrelationId);
                return BadRequest(new PdfResultBinary { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Html to pdf conversion operation failed.", requestModel?.CorrelationId);
                return StatusCode((int)HttpStatusCode.InternalServerError, new PdfResultBinary { Success = false, Message = ex.Message });
            }
            finally
            {
                if (requestModel != null)
                    requestModel.Content = null;
            }
        }

        [HttpPost("HtmlToImage")]
        [SwaggerOperation(OperationId = "HtmlToImage")]
        [SwaggerGroup("BinaryConversion")]
        [RequestSizeLimit((long)7 * 1024 * 1024)]
        public async Task<ActionResult<PdfResultBinary[]>> ConvertHtmlToImage([FromBody] HtmlToPdfModelBinary requestModel)
        {
            try
            {
                if (requestModel == null || requestModel.Content == null)
                    return BadRequest(new PdfResultBinary[] { new PdfResultBinary { Success = false, Message = "Request data can not be null" } });

                var converterService = SelectPdfConverterService(requestModel, out var pdfConverterType);

                PdfResultBinary[] result = await converterService.ConvertHtmlToImage(requestModel);

                requestModel.Content = null;

                return Ok(result); // ASP.NET Core will serialize as MessagePack

            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(ex, "Html to pdf conversion operation failed.", requestModel?.CorrelationId);
                return BadRequest(new PdfResultBinary { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Html to pdf conversion operation failed.", requestModel?.CorrelationId);
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

            if (model.Content.Length > _appSettings.RequestLimit * 1024)
            {
                _logger.Info($"Pdf Belge Boyutu izin verilenin üzerinde. (Size Of Document: {model.Content.Length}), Convertor override ediliyor. {PdfConverterType.WkHtmlToPDF}", null, model.CorrelationId);
                converterService = _converterList[PdfConverterType.WkHtmlToPDF];
            }

            return converterService;
        }


    }
}