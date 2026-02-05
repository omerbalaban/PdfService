using eLogo.LogProvider;
using System;

namespace eLogo.PdfService.Settings.PaasSettings
{
    public class AppSettings
    {

        public RegistrationInfo RegistrationInfo { get; set; } = new RegistrationInfo();

        public string ApiKey { get; set; } = string.Empty;
        public bool TransactionCounterLogEnable { get; set; } = true;
        public long LargePdfLimitAsByte { get; set; } = (long)(5.5 * 1024);
        public long OversizePdfLimit { get; set; } = (long)(2 * 1024);

        public string MongoDbConnectionString { get; set; }
       
        public string FluentdHostName { get; set; }
        public int FluentdPort { get; set; }
        public string LogProjectCode { get; set; } = "LogMessages";
        public string LogSourceName { get; set; } = "PdfService";
        public string ApplicationName { get; set; } = "PdfServiceApi";

        public string IronPdfSerialKey { get; set; } = string.Empty;
        public int RenderTimeout { get; set; } = 180; //as second
        public int RenderTaskTimeout { get; set; } = 180; //as second
        public bool EnableJavaScript { get; set; } = false; 
        public int IronPdfLogLevel { get; set; } = 0;
        public int ZoomFactor { get; set; } = 95;

    }

    public class RegistrationInfo
    {
        public string Version { get; set; } = "1.0.0";
        public DateTime Date { get; set; }
        public string Host { get; set; }
        public string Application { get; set; }
    }


}
