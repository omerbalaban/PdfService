using eLogo.LogProvider;
using System;

namespace eLogo.PdfService.Settings.PaasSettings
{
    public class AppSettings
    {

        public RegistrationInfo RegistrationInfo { get; set; } = new RegistrationInfo();
        public string HIQPdfSerialNumber { get; set; } = "YCgJMTAE-BiwJAhIB-EhlWTlBA-UEBRQFBA-U1FOUVJO-WVlZWQ==";

        public string IronPdfSerialKey { get; set; } = "IRONPDF.HAKANAKAÇAY.19132-689C2974B5-PDBMTSF6YESI7ZA7-AVUZCTPB7CUP-RURFETUS5EUE-ZDHNLKGTV6NO-PNZA22OFYB44-B4K5SD-TPML3LKLAY2CUA-DEPLOYMENT.TRIAL-HZASOQ.TRIAL.EXPIRES.27.NOV.2021";

        public long RequestLimit { get; set; } = (long)(5.5 * 1024);

        public WkhtmlPdfSettings wkhtmlPdfSettings { get; set; } = new WkhtmlPdfSettings();

        public string MongoDbConnectionString { get; set; }

        public string LogConnectionString { get; set; }
        public string FluentdHostName { get; set; }
        public int FluentdPort { get; set; }
        public string LogProjectCode { get; set; } = "LogMessages";
        public string LogSourceName { get; set; } = "PdfService";

        public LogLevel MongoLogLevel { get; set; } = LogLevel.Warn;
        public LogLevel FluentLogLevel { get; set; } = LogLevel.Info;


        public int RenderTimeout { get; set; } = 180; //as second
        public int RenderTaskTimeout { get; set; } = 180; //as second

        public int DefaultProvider { get; set; } = 10;

        public int IronPdfLogLevel { get; set; } = 0;

        public int ZoomFactor { get; set; } = 95;

        public bool EnableIronPdfDebug { get; set; } = false;

        public bool HtmlTraceEnable { get; set; } = false;

        public bool AuthenticationEnable { get; set; } = true;

        public bool TransactionLogCounterEnable { get; set; } = true;

        public string Database { get; set; } = "PdfProvider";

        public string ApplicationName { get; set; } = "PdfServiceApi";

        public bool IsEmpty()
        {
            return false;
        }

    }


    public class WkhtmlPdfSettings 
    {
        public int ImageQuality { get; set; } = 70;

        public int DPI { get; set; } = 70;

        public int ImageDPI { get; set; } = 450;

        public string WkhtmlToPdfServiceUrl { get; set; } = "http://localhost:8000/api/Pdf/ConvertHtmlToPdf";

        public bool IsEmpty()
        {
            return true;
        }
    }

    public class RegistrationInfo
    {
        public string Version { get; set; } = "1.0.0";
        public DateTime Date { get; set; }
        public string Host { get; set; }
        public string Application { get; set; }
    }


}
