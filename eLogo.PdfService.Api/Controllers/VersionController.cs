using eLogo.PdfService.Settings;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Net;

namespace eLogo.PdfService.Api.Controllers
{

    [Route("api/Version")]
    [ApiController]
    public class VersionController : ControllerBase
    {

        private static AppSettings _appSettings;

        public VersionController(AppSettings appSettings)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        }


        [HttpGet]
        [Produces("application/json")]
        public IActionResult GetVersion()
        {
            try
            {
                var version = GetServiceVersionResult();
                return Ok(version);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        public static VersionModel GetServiceVersionResult()
        {
            FileVersionInfo fileVersionInfo = GetEntryApplicationFileVersion();
            string version = fileVersionInfo.FileVersion;
            return new VersionModel { ApiVersion = "v2", ReleaseVersion = version, ApplicationName = _appSettings.ApplicationName };
        }

        public static FileVersionInfo GetEntryApplicationFileVersion()
        {
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            if (entryAssembly == null)
                throw new InvalidOperationException("Entry assembly is not available.");
            return FileVersionInfo.GetVersionInfo(entryAssembly.Location);
        }
    }

}


public class VersionModel
{
    public string ApplicationName { get; set; }
    public string ApiVersion { get; set; }
    public string ReleaseVersion { get; set; }
}

