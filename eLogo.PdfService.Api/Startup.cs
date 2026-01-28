using eLogo.PdfService.Api.Diagnostics;
using eLogo.PdfService.Api.Middleware;
using eLogo.PdfService.Services;
using eLogo.PdfService.Services.Domain.Collections;
using eLogo.PdfService.Services.Domain.Collections.Interfaces;
using eLogo.PdfService.Services.Interfaces;
using eLogo.PdfService.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using NAFCore.Common.Log2Fluentd;
using NAFCore.Common.Utils.Diagnostics.Logger;
using NAFCore.Diagnostics.Model.Interfaces;
using NAFCore.DistributedCaching;
using NAFCore.Platform.Services.Hosting.Types;
using NAFCore.Settings;
using NAFCore.Settings.UI.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace eLogo.PdfService.Api
{
    /// <summary>
    /// 
    /// </summary>
    public class Startup : WebHostStartup
    {
        private readonly IWebHostEnvironment _env;
        private AppSettings _settings;
        private readonly IConfiguration _configuration;
        /// <summary>
        /// Constcurtor Startup
        /// </summary>
        /// <param name="env"></param>
        public Startup(IWebHostEnvironment env) : base(env)
        {
            _env = env;
            this._configuration = new ConfigurationBuilder().AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")).Build();
        }
        /// <summary>
        /// Generation Of Security Secret Key
        /// </summary>
        /// <returns></returns>
        protected override Guid BuildSecuritySecret()
        {
            return new Guid("10704A48-B982-4376-B6EB-D9C47B0AD7AD");
        }
        /// <summary>
        /// Configure Diagnostics
        /// </summary>
        /// <param name="services"></param>
        protected override void DoConfigureDiagnostics(IServiceCollection services)
        {
            NLogger.Instance().LogToConsole().Warn("0-DoConfigureDiagnostics");

            services.AddSingleton<PdfServiceDiagnosisService>();

            _settings = new AppSettings();
            // initialize settings
            _settings = NAFSettings.Current.ReadAppSettings<AppSettings>(throwIfNotFound: false) ?? new AppSettings();

            services.AddSingleton<IDiagnosisService>(sp =>
            {
                var service = sp.GetRequiredService<IIronPdfConverter>();

                return new PdfServiceDiagnosisService(service);
            });


        }

        protected override bool AutoRegisterSecuritySecret()
        {
            return false;
        }

        /// <summary>
        ///  Adding Services
        /// </summary>
        /// <param name="services"></param>
        protected override void DoAddServices(IServiceCollection services)
        {
            NLogger.Instance().LogToConsole().Warn("1-DoAddServices");

            services.AddSettingsUI<SettingsUIPathOptions>();
            services.AddNAFDistributedCaching();

            _settings = new AppSettings();
            // initialize settings
            _settings = NAFSettings.Current.ReadAppSettings<AppSettings>(throwIfNotFound: false) ?? new AppSettings();
            services.AddSingleton(_settings);

            // Add Response Compression with Gzip support
            services.AddResponseCompression(options =>
             {
                 options.EnableForHttps = true;
                 options.Providers.Add<BrotliCompressionProvider>();
                 options.Providers.Add<GzipCompressionProvider>();
             });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;       
            });


        }

        /// <summary>
        /// Initialization Application Services
        /// </summary>
        /// <param name="services"></param>
        protected override void InitializeApp(IServiceCollection services)
        {
            NLogger.Instance().LogToConsole().Warn("2-InitializeApp");
            try
            {
                if (_settings.IsEmpty())
                    NLogger.Instance().LogToConsole().Warn("Application settings are not configured. Make sure your settings are configured.");

                RegisterDbContext(services);

                RegisterServices(services);

                RegisterIronPdf(services);

                services.AddMvc();

                services.AddControllers(options =>
                {
                    options.InputFormatters.Insert(0, new MessagePack.AspNetCoreMvcFormatter.MessagePackInputFormatter());
                    options.OutputFormatters.Insert(0, new MessagePack.AspNetCoreMvcFormatter.MessagePackOutputFormatter());
                    // This makes sure the API returns a 406 if the client requests an unsupported format
                    //options.ReturnHttpNotAcceptable = true;
                });

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Uygulama Baslatilamadi" + ex.StackTrace);
                NLogger.Instance().Fatal(ex, "Uygulama Baslatilamadi");
            }
            ShowSettings();
        }

        //bunu eklemezek swagger çalışmıyor
        /// <summary>
        /// Adds the source XML.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="excludedPaths">The excluded paths.</param>
        protected override void AddSourceXML(SwaggerGenOptions options, List<string> excludedPaths)
        {
            base.AddSourceXML(options, new List<string> { "" });
        }


        private void RegisterServices(IServiceCollection services)
        {
            NLogger.Instance().LogToConsole().Warn("Registering services...");
            //services.AddScoped<IIronPdfConverter, IronPdfConverterService>();
            services.AddScoped<IWkhtmlConvertService, WkhtmlConverterService>();
            services.AddScoped<ICompressService, CompressService>();
            //services.AddScoped(s => ActivatorUtilities.CreateInstance<IronPdfDiagnoseService>(s, s.GetRequiredService<IIronPdfConverter>()));

            services.AddScoped<IImageResizer, ImageResizer>();

        }

        private void RegisterIronPdf(IServiceCollection services)
        {
            NLogger.Instance().LogToConsole().Warn("Registering IronPdf...");

            IronPdf.License.LicenseKey = _settings.IronPdfSerialKey;
            IronPdf.Logging.Logger.EnableDebugging = _settings.EnableIronPdfDebug;
            IronPdf.Logging.Logger.LoggingMode = (IronPdf.Logging.Logger.LoggingModes)_settings.IronPdfLogLevel;
            IronPdf.Installation.ChromeGpuMode = IronPdf.Engines.Chrome.ChromeGpuModes.Disabled;
            IronPdf.Installation.LinuxAndDockerDependenciesAutoConfig = true;
            IronPdf.Installation.Initialize();

            services.AddSingleton<IIronPdfConverter, IronPdfConverterService>();
            services.AddSingleton(s => ActivatorUtilities.CreateInstance<IronPdfDiagnoseService>(s, s.GetRequiredService<IIronPdfConverter>()));

        }

        /// <summary>
        /// Startup services being used
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        protected override void DoUseStartServices(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
        {
            if (!_env.IsProduction()) { }

            NLogger.Instance().LogToConsole().Warn("3-DoUseStartServices");

            // Enable response compression (must be before other middleware)
            app.UseResponseCompression();

            // Add custom middleware to handle gzip request decompression
            app.UseMiddleware<GzipRequestDecompressionMiddleware>();

            app.UseSettingsUI("/api/settings/");
            app.UseNAFDistributedCaching();
            app.UseMvc();


        }

        /// <summary>
        /// Startup DBContext being used
        /// </summary>
        /// <param name="services"></param>
        private bool RegisterDbContext(IServiceCollection services)
        {
            NLogger.Instance().LogToConsole().Warn("Registering DbContext...");

            string mongoDbConnectionString = _settings.ConnectionString;

            //NLogger.Instance().Info("MongoDB Connection String : " + mongoDbConnectionString);

            MongoClient mongoClient = new MongoClient(mongoDbConnectionString);
            services.AddSingleton<IMongoClient>((imp) => mongoClient);

            NLogger.Instance().LogToConsole().Warn("Registering Repositories...");

            services.AddScoped<IPdfTransactionCollection>(s => ActivatorUtilities.CreateInstance<PdfTransactionCollection>(s, s.GetRequiredService<IMongoClient>(), _settings.Database));
            services.AddScoped<IApiKeyCollection>(s => ActivatorUtilities.CreateInstance<ApiKeyCollection>(s, s.GetRequiredService<IMongoClient>(), _settings.Database));
            return true;
        }

        /// <summary>
        /// Logger Configuration
        /// </summary>
        protected override void DoConfigureLogger()
        {
            FluentdExtensions.AddFluentdLog(NAFSettings.Current, GetType().GetTypeInfo().Assembly);
        }
        void ShowSettings()
        {
            if (!_env.IsDevelopment())
                return;

            Console.WriteLine($"Security Secret : {BuildSecuritySecret()}");
            Console.WriteLine(new string('-', 40));

            var attributes = Assembly.GetEntryAssembly().GetCustomAttributes();

            foreach (Attribute item in attributes)
            {
                if (item.GetType() != typeof(NAFCore.Common.Attributes.NAFSecurityIdAttribute))
                    continue;
                NAFCore.Common.Attributes.NAFSecurityIdAttribute nafCoreSecAttr = item as NAFCore.Common.Attributes.NAFSecurityIdAttribute;
                Console.WriteLine($"NAF Security ID : {nafCoreSecAttr.SecurityId} -  {nafCoreSecAttr.Group}");
            }
        }
    }
}