using eLogo.LogProvider.Interface;
using eLogo.PdfService.Models;
using eLogo.PdfService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace eLogo.PdfService.Api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        private readonly IServiceLogger _logger;
        private readonly Dictionary<PdfConverterType, IPdfConvertService> _converterList;
        private readonly ICompressService _compressService;

        /// <summary>
        /// 
        /// </summary>
        public PdfController(IServiceLogger logger, IIronPdfConverter ironPdfService, ICompressService compressService, IWkhtmlConvertService wkhtmlConvertService)
        {
            _converterList = new Dictionary<PdfConverterType, IPdfConvertService>
            {
                { PdfConverterType.Default, ironPdfService },
                { PdfConverterType.IronPdfConverter, ironPdfService },
                { PdfConverterType.WkHtmlToPDF,wkhtmlConvertService }
            };

            _compressService = compressService;
            _logger = logger;
        }


        /// <summary>
        /// Converts html to pdf
        /// </summary>
        /// <returns></returns>
        [HttpPost("ConvertHtmlToPdf")]
        [Produces("application/json")]
        [RequestSizeLimit((long)7 * 1024 * 1024)]
        public async Task<ActionResult<PdfResult>> ConvertHtmlToPdf(HtmlToPdfModel model)
        {
            byte[] binaryDocumentData = null;

            var converterService = _converterList[PdfConverterType.Default];
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Base64HtmlContent))
                    throw new InvalidOperationException("Base64HtmlContent can not be null or empty.");

                binaryDocumentData = Convert.FromBase64String(model.Base64HtmlContent);
                _logger.Info($"Pdf convert request received {model.CorrelationId} Size Of Document : {binaryDocumentData.Length}", null, model.CorrelationId);

                var converterModel = new HtmlToPdfModelBinary
                {
                    Content = binaryDocumentData,
                    CorrelationId = model.CorrelationId,
                    Margins = model.Margins,
                    OpenPassword = model.OpenPassword,
                    PageOrientation = model.PageOrientation,
                    PageSize = model.PageSize,
                    PdfConverter = model.PdfConverter,
                    Attachments = model.Attachments?.Select(s => new AttachmentModelBinary { FileName = s.FileName, Data = Convert.FromBase64String(s.Data) }).ToArray(),
                    CustomPropertyItems = model.CustomPropertyItems?.Select(s => new PropertyItemBinary { Key = s.Key, Value = s.Value }).ToArray(),
                    DocumentTitle = model.DocumentTitle,
                    Zoom = model.Zoom,
                };

                converterService = SelectPdfConverterService(converterModel, out var pdfConverterType);

                var result = await converterService.ConvertHtmlToPdf(converterModel);

                _logger.Info($"Pdf conversion completed {pdfConverterType} ", null, model.CorrelationId);

                // Nullify large objects to help GC
                binaryDocumentData = null;
                converterModel.Content = null;

                return new PdfResult
                {
                    Content = Convert.ToBase64String(result.Content),
                    Id = result.Id,
                    Message = result.Message,
                    Success = result.Success,
                };

            }
            catch (InvalidOperationException ex)
            {
                _logger.Error($"Html to pdf conversion is failed. ConverterService: {converterService}", ex);
                return BadRequest(new PdfResult { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.Error($"Html to pdf conversion is failed. ConverterService: {converterService}", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
            finally
            {
                binaryDocumentData = null;
            }
        }


        /// <summary>
        /// Converts html to pdf
        /// </summary>
        /// <returns></returns>
        [HttpPost("ConvertHtmlToImage")]
        [Produces("application/json")]
        [RequestSizeLimit((long)7 * 1024 * 1024)]
        public async Task<ActionResult<PdfResult[]>> ConvertHtmlToImage(HtmlToPdfModel model)
        {
            byte[] binaryDocumentData = null;
            var converterService = _converterList[PdfConverterType.Default];
            try
            {
                _logger.Info($"Pdf convert request received", null, model.CorrelationId);

                if (model == null || string.IsNullOrWhiteSpace(model.Base64HtmlContent))
                    throw new InvalidOperationException("Base64HtmlContent can not be null or empty.");

                binaryDocumentData = Convert.FromBase64String(model.Base64HtmlContent);

                var converterModel = new HtmlToPdfModelBinary
                {
                    Content = binaryDocumentData,
                    CorrelationId = model.CorrelationId,
                    Margins = model.Margins,
                    OpenPassword = model.OpenPassword,
                    PageOrientation = model.PageOrientation,
                    PageSize = model.PageSize,
                    PdfConverter = model.PdfConverter,
                    Attachments = model.Attachments?.Select(s => new AttachmentModelBinary { FileName = s.FileName, Data = Convert.FromBase64String(s.Data) }).ToArray(),
                    CustomPropertyItems = model.CustomPropertyItems?.Select(s => new PropertyItemBinary { Key = s.Key, Value = s.Value }).ToArray(),
                    DocumentTitle = model.DocumentTitle,
                    Zoom = model.Zoom,
                };

                converterService = SelectPdfConverterService(converterModel, out var pdfConverterType);

                var result = await converterService.ConvertHtmlToImage(converterModel);


                _logger.Info($"Pdf conversion completed {pdfConverterType} ", null, model.CorrelationId);

                // Nullify large objects to help GC
                binaryDocumentData = null;
                converterModel.Content = null;

                return result.Select(s => new PdfResult
                {
                    Content = Convert.ToBase64String(s.Content),
                    Id = s.Id,
                    Message = s.Message,
                    Success = s.Success,
                }).ToArray();
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error($"Html to pdf conversion is failed. ConverterService: {converterService}", ex);
                return BadRequest(new PdfResult { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.Error($"Html to pdf conversion is failed. ConverterService: {converterService}", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
            finally
            {
                binaryDocumentData = null;
            }
        }

        /// <summary>
        /// Converts html to pdf
        /// </summary>
        /// <returns></returns>
        [HttpPost("ConvertHtmlToPdfBinary")]
        [RequestSizeLimit((long)7 * 1024 * 1024)]
        public async Task<ActionResult<byte[]>> ConvertHtmlToPdfBinary(byte[] serializedModel)
        {
            HtmlToPdfModelBinary model = null;
            var converterService = _converterList[PdfConverterType.Default];
            try
            {
                _logger.Info($"Pdf convert request received serialized data: {serializedModel.Length}");

                model = MessagePack.MessagePackSerializer.Deserialize<HtmlToPdfModelBinary>(serializedModel);

                if (model == null || model.Content == null)
                    throw new InvalidOperationException("Model or Html data can not be null");

                _logger.Info($"Pdf convert model", null, model.CorrelationId);

                converterService = SelectPdfConverterService(model, out var pdfConverterType);

                var result = await converterService.ConvertHtmlToPdf(model);

                _logger.Debug($"Pdf conversion completed  {pdfConverterType} ", null, model.CorrelationId);

                // Nullify large objects to help GC
                model.Content = null;

                return File(MessagePack.MessagePackSerializer.Serialize(result), "application/octet-strem");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new PdfResult { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.Error($"Html to pdf conversion operation is failed. ConverterService: {converterService}", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
            finally
            {
                if (model != null) model.Content = null;
            }
        }

        /// <summary>
        /// Converts html to pdf
        /// </summary>
        /// <returns></returns>
        [HttpPost("ConvertHtmlToPdfCompressed")]
        [RequestSizeLimit((long)7 * 1024 * 1024)]
        public async Task<ActionResult<byte[]>> ConvertHtmlToPdfCompressed(byte[] compressedModel)
        {
            HtmlToPdfModelBinary model = null;
            byte[] serializedModel = null;
            var converterService = _converterList[PdfConverterType.Default];
            try
            {
                _logger.Info($"Pdf convert request received compressed Data: {compressedModel.Length}");

                serializedModel = _compressService.Decompress(compressedModel);
                model = MessagePack.MessagePackSerializer.Deserialize<HtmlToPdfModelBinary>(serializedModel);

                if (model == null || model.Content == null)
                    throw new InvalidOperationException("Model or Html data can not be null");

                _logger.Info($"Pdf convert model: {compressedModel.Length}", null, model.CorrelationId);

                converterService = SelectPdfConverterService(model, out var pdfConverterType);

                var result = await converterService.ConvertHtmlToPdf(model);

                var responseModel = MessagePack.MessagePackSerializer.Serialize(result);

                _logger.Debug($"Pdf conversion completed  {pdfConverterType} ", null, model.CorrelationId);

                // Nullify large objects to help GC
                model.Content = null;
                serializedModel = null;

                return File(_compressService.Compress(responseModel), "application/octet-stream");
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error($"Html to pdf conversion is failed. ConverterService: {converterService}", ex);
                return BadRequest(new PdfResult { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.Error($"Html to pdf conversion is failed. ConverterService: {converterService}", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
            finally
            {
                if (model != null) model.Content = null;
                serializedModel = null;
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

            if (model.Content.Length > Settings.Settings.AppSetting.ApiRequestLimit * 1024)
            {
                _logger.Info($"Pdf Belge Boyutu izin verilenin üzerinde. (Size Of Document: {model.Content.Length}), Convertor override ediliyor. {PdfConverterType.WkHtmlToPDF}", null, model.CorrelationId);
                converterService = _converterList[PdfConverterType.WkHtmlToPDF];
            }

            return converterService;
        }

    }
}