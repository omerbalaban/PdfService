using eLogo.LogProvider.Interface;
using eLogo.PdfService.Models;
using eLogo.PdfService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
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
    public class PdfBinaryController : ControllerBase
    {
        private readonly IServiceLogger _logger;
        private readonly Dictionary<PdfConverterType, IPdfConvertService> _converterList;

        public PdfBinaryController(IServiceLogger logger, IIronPdfConverter ironPdfService, IWkhtmlConvertService wkhtmlConvertService)
        {
            _converterList = new Dictionary<PdfConverterType, IPdfConvertService>
            {
                { PdfConverterType.Default, ironPdfService },
                { PdfConverterType.IronPdfConverter, ironPdfService },
                { PdfConverterType.WkHtmlToPDF, wkhtmlConvertService }
            };

            _logger = logger;
        }

        [HttpPost("ConvertHtmlToPdf")]
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

        [HttpPost("ConvertHtmlToImage")]
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

            if (model.Content.Length > Settings.Settings.AppSetting.RequestLimit * 1024)
            {
                _logger.Info($"Pdf Belge Boyutu izin verilenin üzerinde. (Size Of Document: {model.Content.Length}), Convertor override ediliyor. {PdfConverterType.WkHtmlToPDF}", null, model.CorrelationId);
                converterService = _converterList[PdfConverterType.WkHtmlToPDF];
            }

            return converterService;
        }


    }
}