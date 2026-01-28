using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace eLogo.PdfService.Settings.PaasSettings
{

    public class AppInfo
    {
        public string GroupName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool IsActive { get; set; }
    }
}