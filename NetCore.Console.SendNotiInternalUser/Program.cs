using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetCore.Business;
using NetCore.Data;
using NetCore.DataLog;
using NetCore.Shared;
using Serilog;
using Serilog.Exceptions;
using System;
using System.Threading.Tasks;


namespace NetCore.Console.SendNotiInternalUser
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                System.Console.WriteLine($"Console start at: {DateTime.Now.ToString("dd-MM-yyyy HH:mm")}");

                IConfiguration configuration = new ConfigurationBuilder()
                     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables()
                     .AddCommandLine(args)
                     .Build();

                // Configure serilog
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithExceptionDetails()
                    .Enrich.WithMachineName()
                    .CreateLogger();
                Log.Information("Console service started at " + DateTime.Now.ToString());

                var isTesting = (Utils.GetConfig("ConnectionString:IsTesting") == "HAS_TEST");
                var databaseType = Utils.GetConfig("ConnectionString:DbType");
                var connectionString = Utils.GetConfig("ConnectionString:" + databaseType);

                var serviceProvider = new ServiceCollection()
                    .AddLogging()
                    .AddSingleton<ICacheService, InMemoryCacheService>()
                    .AddDbContext<DataContext>(x => x.UseNpgsql(connectionString))
                    .Configure<MongoDBDatabaseSettings>(
                    configuration.GetSection(nameof(MongoDBDatabaseSettings)))
                    .AddSingleton<IMongoDBDatabaseSettings>(sp =>
                        sp.GetRequiredService<IOptions<MongoDBDatabaseSettings>>().Value)
                    .AddTransient<IEmailHandler, EmailHandler>()
                    .AddTransient<ICallStoreHelper, CallStoreHelper>()
                    .AddSingleton<INotifyHandler, NotifyHandler>()
                    .AddSingleton<IOrganizationHandler, OrganizationHandler>()
                    .AddSingleton<IUserRoleHandler, UserRoleHandler>()
                    .AddSingleton<IOrganizationConfigHandler, OrganizationConfigHandler>()
                    .AddSingleton<ISystemNotifyHandler, SystemNotifyHandler>()
                .BuildServiceProvider();

                var notifyHandler = serviceProvider.GetService<ISystemNotifyHandler>();

                //Excute(notifyHandler).Wait();
                notifyHandler.PushNotificationRemindSignDocumentDaily(new SystemLogModel()
                {
                    TraceId = Guid.Empty.ToString()
                }).Wait();

                System.Console.WriteLine("Send noti complete");

            }
            catch (Exception ex)
            {
                Log.Error("Job Error: " + ex.Message);
            }

            System.Threading.Thread.Sleep(5000);
        }
    }
}
