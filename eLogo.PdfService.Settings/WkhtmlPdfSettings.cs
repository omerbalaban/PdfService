using NAFCore.Common.Attributes.Settings;
using NAFCore.Common.Types.Config;

namespace eLogo.PdfService.Settings
{
    public class WkhtmlPdfSettings : IConfigValidation
    {
        [NSkip]
        public int ImageQuality { get; set; } = 70;

        [NSkip]
        public int DPI { get; set; } = 70;

        [NSkip]
        public int ImageDPI { get; set; } = 450;

        public string WkhtmlToPdfServiceUrl { get; set; } = "http://localhost:8000/api/Pdf/ConvertHtmlToPdf";

        public bool IsEmpty()
        {
            return true;
        }
    }
}
