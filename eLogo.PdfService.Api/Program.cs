using eLogo.LogProvider;
using eLogo.LogProvider.Interface;
using eLogo.PdfService.Api.Middleware;
using eLogo.PdfService.Services;
using eLogo.PdfService.Services.Domain.Collections;
using eLogo.PdfService.Services.Domain.Collections.Interfaces;
using eLogo.PdfService.Services.Interfaces;
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

            RegisterIronPdf(builder);

            // Configure MVC
            builder.Services.AddControllers(options =>
            {
                options.InputFormatters.Insert(0, new MessagePack.AspNetCoreMvcFormatter.MessagePackInputFormatter());
                options.OutputFormatters.Insert(0, new MessagePack.AspNetCoreMvcFormatter.MessagePackOutputFormatter());
            });

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            RegisterSwagger(builder);

            var app = builder.Build();
            Trace.TraceInformation($"Environment: {app.Environment.EnvironmentName}");

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

            Trace.TraceInformation("=================================================");
            Trace.TraceInformation("  ✓ Application Ready");
            Trace.TraceInformation("=================================================");

            await app.RunAsync();
        }

        //private static async Task InitializeMongoDbAsync(WebApplication app)
        //{
        //    Trace.TraceInformation("Starting MongoDB initialization...");

        //    try
        //    {
        //        using var scope = app.Services.CreateScope();
        //        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        //        var initializer = new MongoDbInitializer(database);

        //        await initializer.InitializeAsync();

        //        Trace.TraceInformation("MongoDB indexes initialized successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.TraceError($"MongoDB initialization failed: {ex.Message}");
        //        // Production'da uygulama başlamasını engellemek isterseniz throw edebilirsiniz
        //        // throw;
        //    }
        //}

        private static void ConfigureTracing()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.AutoFlush = true;
        }

        //private static void RegisterApiVersioning(WebApplicationBuilder builder)
        //{
        //    Trace.TraceInformation("Starting API Versioning registration.");

        //    builder.Services.AddApiVersioning(options =>
        //    {
        //        options.DefaultApiVersion = new ApiVersion(1, 0);
        //        options.AssumeDefaultVersionWhenUnspecified = true;
        //        options.ReportApiVersions = true;
        //        options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(), new HeaderApiVersionReader("X-Api-Version"), new QueryStringApiVersionReader("api-version"));
        //    })
        //    .AddApiExplorer(options =>
        //    {
        //        options.GroupNameFormat = "'v'V";
        //        options.SubstituteApiVersionInUrl = true;
        //    });

        //    Trace.TraceInformation("API Versioning registered.");
        //}


        private static void RegisterSwagger(WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }

        private static void RegisterDbContext(WebApplicationBuilder builder)
        {
            var connectionString = Settings.Settings.AppSetting.MongoDbConnectionString;
            var database = Settings.Settings.AppSetting.Database;
            
            Trace.TraceInformation($"MongoDB: {MaskConnectionString(connectionString)} / {database}");

            MongoClient mongoClient = new MongoClient(connectionString);
            builder.Services.AddSingleton<IMongoClient>((imp) => mongoClient);

            builder.Services.AddScoped<IPdfTransactionCollection>(s => ActivatorUtilities.CreateInstance<PdfTransactionCollection>(s, s.GetRequiredService<IMongoClient>(), database));
            builder.Services.AddScoped<IApiKeyCollection>(s => ActivatorUtilities.CreateInstance<ApiKeyCollection>(s, s.GetRequiredService<IMongoClient>(), database));
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

        /// <summary>
        /// Registers application-specific services
        /// </summary>
        private static void RegisterApplicationServices(WebApplicationBuilder builder)
        {
            // Stateless utility services - Singleton for best performance
            // IMPORTANT: These MUST be thread-safe and stateless
            builder.Services.AddSingleton<ICompressService, CompressService>();
            builder.Services.AddSingleton<IImageResizer, ImageResizer>();

            Trace.TraceInformation("Services: Utility services registered (Singleton)");
        }

        private static void RegisterIronPdf(WebApplicationBuilder builder)
        {
            IronPdf.License.LicenseKey = Settings.Settings.AppSetting.IronPdfSerialKey;
            IronPdf.Logging.Logger.EnableDebugging = Settings.Settings.AppSetting.EnableIronPdfDebug;
            IronPdf.Logging.Logger.LoggingMode = (IronPdf.Logging.Logger.LoggingModes)Settings.Settings.AppSetting.IronPdfLogLevel;
            IronPdf.Installation.ChromeGpuMode = IronPdf.Engines.Chrome.ChromeGpuModes.Disabled;
            IronPdf.Installation.LinuxAndDockerDependenciesAutoConfig = true;
            
            IronPdf.Installation.Initialize();
            Trace.TraceInformation($"IronPdf: {(IronPdf.License.IsLicensed ? "Licensed" : "Trial")}");

            builder.Services.AddSingleton<IIronPdfConverter, IronPdfConverterService>();
        }

        /// <summary>
        /// Masks sensitive information in connection strings for safe logging
        /// </summary>
        private static string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "[Empty]";

            try
            {
                // Mask password in MongoDB connection string
                // Format: mongodb://username:password@host:port/database
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
                // If parsing fails, mask the entire string except scheme
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