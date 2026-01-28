using NAFCore.Common.Types.Basic;

namespace eLogo.PdfService.Settings
{
    public class AppServerSettings : NAFCore.Settings.Types.ServerSettings
    {
        public AppServerSettings() : base()
        {
            base.PortRange = new NAFRange<ushort>() { Start = 8100, End = 8199 };
        }

        public override NAFRange<ushort> PortRange { get => base.PortRange; set => base.PortRange = value; }
    }
}
