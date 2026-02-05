using eLogo.LogProvider;
using eLogo.LogProvider.Interface;
using eLogo.PdfService.Api.Middleware;
using eLogo.PdfService.Services;
using eLogo.PdfService.Services.Interfaces;
using eLogo.PdfService.Services.Repositories;
using eLogo.PdfService.Settings.PaasSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;
using eLogo.LogProvider.Helper;
using eLogo.PdfService.Services.Infrastructure;
using System;

namespace eLogo.PdfService.Api
{
    public class Program
    {

        private static async Task Main(string[] args)
        {
            ConfigureTracing();

            Trace.TraceInformation("=================================================");
            Trace.TraceInformation("  eLogo PDF Service API");
            Trace.TraceInformation("=================================================");

            var psvc = new PaasSettingsService(new AppSettings());
            Settings.Settings.InitializeSettings(psvc.StartAsync().Result);

            var builder = WebApplication.CreateBuilder(args);

            // Register services
            AddServices(builder);
            RegisterFluentdLogger(builder);

            RegisterDbContext(builder);

            RegisterApplicationServices(builder);

            RegisterIronPdf(builder);

            // Configure MVC
            builder.Services.AddControllers(options =>
            {
                options.InputFormatters.Insert(0, new MessagePack.AspNetCoreMvcFormatter.MessagePackInputFormatter());
                options.OutputFormatters.Insert(0, new MessagePack.AspNetCoreMvcFormatter.MessagePackOutputFormatter());
            });

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            RegisterSwagger(builder);

            // Add Health Checks
            builder.Services.AddHealthChecks();

            var app = builder.Build();
            Trace.TraceInformation($"Environment: {app.Environment.EnvironmentName}");

            // Initialize MongoDB Indexes
            await InitializeMongoDbIndexesAsync(app);

            // Configure pipeline
            app.UseResponseCompression();
            app.UseMiddleware<GzipRequestDecompressionMiddleware>();
            app.UseRouting();

            bool swaggerEnabled = builder.Configuration.GetValue<bool>("SwaggerEnabled", true);

            if (app.Environment.IsDevelopment() || swaggerEnabled)
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PdfService API v1");
                    options.RoutePrefix = "swagger";
                });
                Trace.TraceInformation("Swagger: /swagger");
            }

            app.MapControllers();
            app.MapHealthChecks("/health");

            Trace.TraceInformation("Health Endpoint: /health");
            Trace.TraceInformation("=================================================");
            Trace.TraceInformation("  ✓ Application Ready");
            Trace.TraceInformation("=================================================");

            await app.RunAsync();
        }


        private static void ConfigureTracing()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.AutoFlush = true;
        }


        private static void RegisterSwagger(WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }

        private static void RegisterDbContext(WebApplicationBuilder builder)
        {
            var connectionString = Settings.Settings.AppSetting.MongoDbConnectionString;
            var databaseName = LogHelper.GetDbName(connectionString);

            Trace.TraceInformation($"MongoDB: {MaskConnectionString(connectionString)}");

            var mongoClient = new MongoClient(connectionString);
            builder.Services.AddSingleton<IMongoClient>(mongoClient);

            // Register IMongoDatabase
            builder.Services.AddSingleton<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                return client.GetDatabase(databaseName);
            });

            // Register Repositories (Scoped for request lifetime)
            builder.Services.AddScoped<ITransactionTracking, TransactionTracking>();
            builder.Services.AddScoped<IClientCredentialsRepository, ClientCredentialsRepository>();
            builder.Services.AddScoped<IFailedConversionRepository, FailedConversionRepository>();

            Trace.TraceInformation("MongoDB: Repositories registered");
        }

        private static async Task InitializeMongoDbIndexesAsync(WebApplication app)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

                await MongoDbIndexInitializer.CreateIndexesAsync(database);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"MongoDB Index Initialization Failed: {ex.Message}");
                // Don't throw - allow app to start even if indexes fail
                // Indexes can be created manually or retried later
            }
        }

        private static void RegisterFluentdLogger(WebApplicationBuilder builder)
        {
            var appName = Settings.Settings.AppSetting.ApplicationName ?? "PdfService";
            var fluentdHost = Settings.Settings.AppSetting.FluentdHostName;
            var fluentdPort = Settings.Settings.AppSetting.FluentdPort;

            Trace.TraceInformation($"Fluentd: {fluentdHost}:{fluentdPort}");

            builder.Services.AddSingleton<IServiceLogger>(serviceProvider =>
            {
                var httpContextAccessor = serviceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
                var logger = new eLogo.LogProvider.Logger(httpContextAccessor);

                //var mongoSettings = new MongoDBSettings(Settings.Settings.AppSetting.LogConnectionString, true, Settings.Settings.AppSetting.MongoLogLevel);
                var fluentdSettings = new FluentdSettings(fluentdHost, fluentdPort, Settings.Settings.AppSetting.LogSourceName);

                logger.Init(null, fluentdSettings, Settings.Settings.AppSetting.LogProjectCode, Settings.Settings.AppSetting.LogSourceName, appName);

                return logger;
            });
        }

        private static void AddServices(WebApplicationBuilder builder)
        {
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            builder.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });
        }

        private static void RegisterApplicationServices(WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<ICompressService, CompressService>();
            builder.Services.AddSingleton<IImageResizer, ImageResizer>();
            builder.Services.AddScoped<ITransactionTrackingService, TransactionTrackingService>();
            builder.Services.AddScoped<IFailedConversionService, FailedConversionService>();

            // DinkToPdf Converter Registration
            builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

            builder.Services.AddHttpClient();

            builder.Services.AddScoped<IPdfConvertService, WkhtmlConverterService>();

            Trace.TraceInformation("Services: Utility services registered (Singleton)");
            Trace.TraceInformation("Services: DinkToPdf Converter registered");
        }

        private static void RegisterIronPdf(WebApplicationBuilder builder)
        {

            IronPdf.License.LicenseKey = Settings.Settings.AppSetting.IronPdfSerialKey;
            IronPdf.Logging.Logger.LoggingMode = (IronPdf.Logging.Logger.LoggingModes)Settings.Settings.AppSetting.IronPdfLogLevel;
            IronPdf.Installation.ChromeGpuMode = IronPdf.Engines.Chrome.ChromeGpuModes.Disabled;
            IronPdf.Installation.LinuxAndDockerDependenciesAutoConfig = true;
            IronPdf.Installation.TempFolderPath = "/tmp/";

            IronPdf.Installation.Initialize();

            Trace.TraceInformation("Checking IronPdf License");
            if (IronPdf.License.IsLicensed)
                Trace.TraceInformation("  ✓ IronPdf ise Licensed");
            else
                Trace.TraceInformation("  X IronPdf ise Trial");

            builder.Services.AddScoped<IPdfConvertService, IronPdfConverterService>();
        }

        private static string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "[Empty]";

            try
            {
                var uri = new System.Uri(connectionString);
                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    var parts = uri.UserInfo.Split(':');
                    var maskedUserInfo = parts.Length > 1
                        ? $"{parts[0]}:***"
                        : uri.UserInfo;
                    return connectionString.Replace(uri.UserInfo, maskedUserInfo);
                }
                return connectionString;
            }
            catch
            {
                var schemeEnd = connectionString.IndexOf("://");
                if (schemeEnd > 0)
                {
                    return connectionString.Substring(0, schemeEnd + 3) + "***";
                }
                return "***";
            }
        }

    }

}