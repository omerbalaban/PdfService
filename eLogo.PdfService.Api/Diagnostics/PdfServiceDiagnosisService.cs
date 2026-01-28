using eLogo.PdfService.Services;
using eLogo.PdfService.Services.Interfaces;
using NAFCore.Diagnostics.Model;
using NAFCore.Diagnostics.Model.Services;
using System;
using System.Collections.Generic;

namespace eLogo.PdfService.Api.Diagnostics
{
    public class PdfServiceDiagnosisService : DiagnosisService
    {
        private readonly IIronPdfConverter _pdfConverterService;

        public PdfServiceDiagnosisService(IIronPdfConverter pdfConverterService)
        {
            this._pdfConverterService = pdfConverterService;
            Diagnoses = new List<Diagnosis> { new IronPdfDiagnoseService(_pdfConverterService) };
        }

        
    }
}