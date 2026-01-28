using eLogo.PdfService.Settings.PaasSettings;
using System.Configuration;

namespace eLogo.PdfService.Settings
{
    public class Settings
    {

        public static AppSettings AppSetting;

        public static void InitializeSettings(AppSettings appSetting)
        {

            AppSetting = appSetting;
        }

    }
}
