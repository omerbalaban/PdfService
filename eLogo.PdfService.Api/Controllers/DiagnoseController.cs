using eLogo.PdfService.Api.Diagnostics;
using eLogo.PdfService.Models;
using Microsoft.AspNetCore.Mvc;
using NAFCore.Diagnostics.Model;
using NAFCore.Platform.Services.Hosting.APIDoc.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Net;
using System.Threading.Tasks;

namespace eLogo.PdfService.Api.Controllers
{
    [Route("api/Diagnose")]
    [ApiController]
    public class DiagnoseController : ControllerBase
    {
        private readonly IronPdfDiagnoseService _pdfDiagnoseService;

        public DiagnoseController(IronPdfDiagnoseService pdfDiagnoseService)
        {
            _pdfDiagnoseService = pdfDiagnoseService;
        }

        /// <summary>
        /// Converts html to pdf
        /// </summary>
        /// <returns></returns>
        [HttpGet("PdfDiagnose")]
        [SwaggerGroup("Pdf Diagnose")]
        [SwaggerOperation(OperationId = "Pdf Diagnose")]
        [DisableRequestSizeLimit]
        [Produces("application/json")]
        public async Task<ActionResult<DiagnosticResult>> CheckDiagnose()
        {
            try
            {
                return Ok(_pdfDiagnoseService.PerformDiagnosis());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new PdfResult() { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}
