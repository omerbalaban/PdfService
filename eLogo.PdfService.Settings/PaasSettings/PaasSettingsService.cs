using Microsoft.IdentityModel.Tokens;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace eLogo.PdfService.Settings.PaasSettings
{
    public class PaasSettingsService
    {
        const string NAF_SECURITY_ID = "78d0e60a-1c67-4127-aef5-1b166b3d4a20";
        const string PAAS_TITLE = "eLogo.PdfService";
        const string PAAS_DESCRIPTION = "eLogo PdfService";

        private const string APP_VERSION = "1.0.0";
        private const string DEFAULT_VERSION = "1.0.0.0";
        private const string API_SETTINGS_PATH = "api/settings";

        private string PaasPrefix;
        private string PaasGroupName;

        private readonly HttpClient _httpClient;

        private readonly string _baseUrl;
        private AppSettings _appSetting;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            WriteIndented = false,
            PropertyNamingPolicy = null, // Keep original property names
            PropertyNameCaseInsensitive = true
        };

        public PaasSettingsService(AppSettings appSetting)
        {
            _appSetting = appSetting;

            (_baseUrl, PaasPrefix) = ReadPaasSettingInfo();

            if (string.IsNullOrEmpty(_baseUrl))
                throw new ArgumentNullException(nameof(_baseUrl), "PaasSettingsUrl is not configured in appsettings.json or AppSettings");

            _httpClient = new HttpClient();
        }

        private (string baseUrl, string prefix) ReadPaasSettingInfo()
        {
            var prefix = BaseConfigurationManager.ReadString("PaasPrefix");
            var baseUrl = BaseConfigurationManager.ReadString("PaasSettingsUrl");
            PaasGroupName = BaseConfigurationManager.ReadString("PaasGroupName");

            Trace.WriteLine($"PaasSettingsService: BaseUrl={baseUrl}, Prefix={prefix}, GroupName={PaasGroupName}");

            return (baseUrl, prefix);
        }



        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task<AppSettings> StartAsync()
        {
            var appSettings = await GetSettingsAsync(NAF_SECURITY_ID);

            int appVer = int.Parse(APP_VERSION.Replace(".", string.Empty));
            int settingVer = appSettings.RegistrationInfo.Version == null ? appVer - 1 : int.Parse(appSettings.RegistrationInfo.Version.Replace(".", string.Empty));

            if (settingVer < appVer)
            {
                Trace.WriteLine($"Settings will be updated: SettingVer={settingVer} AppVer={appVer}");

                await RegisterAsync(appSettings);
            }

            return appSettings;
        }

        private async Task<AppSettings> GetSettingsAsync(string nafSecurityId)
        {
            try
            {
                var url = $"{_baseUrl}/{API_SETTINGS_PATH}/obj/{nafSecurityId}?prefix={PaasPrefix}";

                Trace.WriteLine($"GetSettings: url={url}");

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var model = JsonSerializer.Deserialize<RegistrationModel>(result, _jsonOptions);

                    return model?.AppSettings ?? throw new ConfigurationErrorsException("Failed to deserialize AppSettings");
                }

                if (SettingNotFound(response))
                {
                    await RegisterAsync(_appSetting);
                    throw new ConfigurationErrorsException($"Settings initial definitions are required");
                }

                throw new ConfigurationErrorsException($"Failed to retrieve settings from settings service {response.StatusCode} {response.Content}");

            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Failed to retrieve settings from settings service", ex);
            }
        }

        private bool SettingNotFound(HttpResponseMessage response)
        {
            try
            {
                var resultContent = response.Content.ReadAsStringAsync().Result;
                var errorContent = JsonSerializer.Deserialize<SettingErrorRoot>(resultContent, _jsonOptions);
                return errorContent != null && errorContent.StatusCode == 500 && (errorContent.Message.Contains("404 (Not Found)") || errorContent.Message.Contains("Value cannot be null. (Parameter 'value')"));
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> RegisterAsync(AppSettings appSettings)
        {
            Trace.WriteLine("Registering settings");

            string postUrl = $"{_baseUrl}/api/settings/registration&securityId={NAF_SECURITY_ID}&version={DEFAULT_VERSION}?prefix={PaasPrefix}";
            try
            {
                appSettings.RegistrationInfo.Date = DateTime.Now;
                appSettings.RegistrationInfo.Host = System.Environment.MachineName;
                appSettings.RegistrationInfo.Application = GetAssemblyName();
                appSettings.RegistrationInfo.Version = APP_VERSION;

                var appInfo = new AppInfo
                {
                    GroupName = PaasGroupName,
                    Title = PAAS_TITLE,
                    Description = PAAS_DESCRIPTION,
                    IsActive = true,
                };

                var registrationModel = new RegistrationModel
                {
                    AppSettings = appSettings,
                    AppInfo = appInfo,
                    Metadata = new Metadata()
                };

                var httpRequestMessage = new HttpRequestMessage
                {
                    Content = new StringContent(SerializeSettings(registrationModel), Encoding.UTF8, "application/json"),
                    RequestUri = new Uri(postUrl),
                    Method = HttpMethod.Post,
                };

                var response = await _httpClient.SendAsync(httpRequestMessage);

                Trace.WriteLine("Registering settings. Response: " + response.StatusCode);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Registering settings exception. " + ex.Message);

                return false;
            }
        }

        private async Task<bool> RemoveAsync(string nafSecurityId, string version = DEFAULT_VERSION)
        {
            try
            {
                var url = $"{_baseUrl}/{API_SETTINGS_PATH}/registration?securityId={NAF_SECURITY_ID}&version={version}";

                var response = await _httpClient.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the application assembly name for registration
        /// </summary>
        /// <returns>The assembly name or "UnknownApplication" if unable to determine</returns>
        private static string GetAssemblyName()
        {
            try
            {
                // Try to get the entry assembly name first (preferred method for .NET 9)
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var assemblyName = entryAssembly.GetName().Name;
                    if (!string.IsNullOrEmpty(assemblyName))
                        return assemblyName;
                }

                // Fallback to the current app domain friendly name
                var appDomainName = AppDomain.CurrentDomain.FriendlyName;
                if (!string.IsNullOrEmpty(appDomainName))
                {
                    // Remove .dll or .exe extension if present
                    return appDomainName.Replace(".dll", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase);
                }

                // Last resort: use calling assembly
                var callingAssembly = Assembly.GetCallingAssembly();
                return callingAssembly?.GetName().Name ?? "UnknownApplication";
            }
            catch (Exception)
            {
                // Return default value if all methods fail
                return "UnknownApplication";
            }
        }

        /// <summary>
        /// Serializes settings object to JSON using System.Text.Json
        /// </summary>
        public string SerializeSettings(object setting)
        {
            return JsonSerializer.Serialize(setting, _jsonOptions);
        }
    }

    public class SettingErrorRoot
    {
        public string ContextID { get; set; }
        public string CorrelationID { get; set; }
        public object HelpLink { get; set; }
        public string Message { get; set; }
        public object Source { get; set; }
        public object StackTrace { get; set; }
        public int StatusCode { get; set; }
    }
}

