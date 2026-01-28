using eLogo.PdfService.Settings;
using NAFCore.Platform.Launcher.Factory;

namespace eLogo.PdfService.Api
{
    public class Program
    {
        /// <summary>
        ///  Main
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {

            LauncherFactory.GetLauncher<Startup, AppServerSettings>().Launch(args);
        }

    }
}
