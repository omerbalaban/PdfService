using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace eLogo.PdfService.Settings.PaasSettings
{
    /// <summary>
    /// Configuration manager for reading appsettings.json values
    /// </summary>
    public static class BaseConfigurationManager
    {
        private static IConfiguration _configuration;

        static BaseConfigurationManager()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        /// <summary>
        /// Read string value from configuration
        /// </summary>
        public static string ReadString(string key, string defaultValue = null)
        {
            return _configuration[key] ?? defaultValue;
        }

        /// <summary>
        /// Read boolean value from configuration
        /// </summary>
        public static bool ReadBool(string key, bool defaultValue = false)
        {
            var value = _configuration[key];
            if (bool.TryParse(value, out bool result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// Read integer value from configuration
        /// </summary>
        public static int ReadInt(string key, int defaultValue = 0)
        {
            var value = _configuration[key];
            if (int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// Read double value from configuration
        /// </summary>
        public static double ReadDouble(string key, double defaultValue = 0.0)
        {
            var value = _configuration[key];
            if (double.TryParse(value, out double result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// Read configuration section
        /// </summary>
        public static IConfigurationSection GetSection(string key)
        {
            return _configuration.GetSection(key);
        }

        /// <summary>
        /// Get full configuration object
        /// </summary>
        public static IConfiguration Configuration => _configuration;
    }
}

