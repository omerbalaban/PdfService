using NAFCore.Common.Attributes.Settings;

namespace eLogo.PdfService.Settings
{
    public class AppSettings
    {
        [NSkip]
        public string HIQPdfSerialNumber { get; set; } = "YCgJMTAE-BiwJAhIB-EhlWTlBA-UEBRQFBA-U1FOUVJO-WVlZWQ==";

        public string IronPdfSerialKey { get; set; } = "IRONPDF.HAKANAKAÇAY.19132-689C2974B5-PDBMTSF6YESI7ZA7-AVUZCTPB7CUP-RURFETUS5EUE-ZDHNLKGTV6NO-PNZA22OFYB44-B4K5SD-TPML3LKLAY2CUA-DEPLOYMENT.TRIAL-HZASOQ.TRIAL.EXPIRES.27.NOV.2021";

        public long RequestLimit { get; set; } = (long)(5.5 * 1024);

        public WkhtmlPdfSettings wkhtmlPdfSettings { get; set; } = new WkhtmlPdfSettings();

        public int RenderTimeout { get; set; } = 180; //as second
        public int RenderTaskTimeout { get; set; } = 180; //as second

        public int DefaultProvider { get; set; } = 10;

        public int IronPdfLogLevel { get; set; } = 0;

        public int ZoomFactor { get; set; } = 95;

        public bool EnableIronPdfDebug { get; set; } = false;

        public bool HtmlTraceEnable { get; set; } = false;

        public string HtmlPath { get; set; } = "C:\\HtmlTemp";

        public bool AuthenticationEnable { get; set; } = true;

        public bool TransactionLogCounterEnable { get; set; } = true;

        public string ConnectionString { get; set; } = "";

        public string Database { get; set; } = "PdfProvider";

        public string ApplicationName { get; set; } = "PdfServiceApi";

        public bool IsEmpty()
        {
            return false;
        }
    }
}
