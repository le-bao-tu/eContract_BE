using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetCore.Shared;
using Sentry;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Display;
using System;
using System.IO;
using System.Text;

namespace NetCore.API
{
    public class Program
    {
        public static IConfiguration Configuration { get; private set; }

        [Obsolete]
        public static void Main(string[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                // Build Configuration
                Configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true,
                        true)
                    .AddCommandLine(args)
                    .AddEnvironmentVariables()
                    .Build();

                #region Config Serilog
                // Configure serilog
                var logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithExceptionDetails()
                    .Enrich.WithMachineName();

                var dsn = Utils.GetConfig("Sentry:dsn");
                if (!string.IsNullOrEmpty(dsn))
                    logger.WriteTo.Sentry(o =>
                         {
                             // Debug and higher are stored as breadcrumbs (default is Information)
                             o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
                             // Warning and higher is sent as event (default is Error)
                             o.MinimumEventLevel = LogEventLevel.Warning;
                             o.Dsn = dsn;
                             o.Release = Utils.GetConfig("Sentry:release");
                             o.Environment = Utils.GetConfig("Sentry:environment");
                             o.AttachStacktrace = true;
                             o.SendDefaultPii = true; // send PII like the username of the user logged in to the device
                             o.TracesSampleRate = Convert.ToDouble(Utils.GetConfig("Sentry:TracesSampleRate"));
                         });
                Log.Logger = logger.CreateLogger();
                #endregion

                Log.Information("API started at " + DateTime.Now.ToString());
                host.Run();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseSerilog();
                    webBuilder.UseSentry();
                });
    }
}
