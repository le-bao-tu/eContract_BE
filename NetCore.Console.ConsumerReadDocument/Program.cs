using System;
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

namespace NetCore.Console.ConsumerReadDocument
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
                    .AddTransient<ISystemApplicationHandler, SystemApplicationHandler>()
                    .AddTransient<ISeedDataHandler, SeedDataHandler>()
                    .AddTransient<IDocumentTypeHandler, DocumentTypeHandler>()
                    .AddTransient<IMetaDataHandler, MetaDataHandler>()
                    .AddTransient<IDocumentTemplateHandler, DocumentTemplateHandler>()
                    .AddTransient<IDashboardHandler, DashboardHandler>()
                    .AddTransient<IDocumentBatchHandler, DocumentBatchHandler>()
                    .AddTransient<IDocumentHandler, DocumentHandler>()
                    // .AddTransient<ISignServiceHandler, SignServiceHandler>()
                    .AddTransient<ISignHashHandler, SignHashHandler>()
                    .AddTransient<ISignDocumentHandler, SignDocumentHandler>()
                    .AddTransient<IOTPHandler, OTPHandler>()
                    .AddTransient<INotifyHandler, NotifyHandler>()
                    .AddTransient<IRoleHandler, RoleHandler>()
                    .AddTransient<INavigationHandler, NavigationHandler>()
                    .AddTransient<IRightHandler, RightHandler>()
                    .AddTransient<IUserRoleHandler, UserRoleHandler>()
                    .AddTransient<IOrganizationConfigHandler, OrganizationConfigHandler>()
                    .AddTransient<IUserSignConfigHandler, UserSignConfigHandler>()
                    .AddTransient<ILayerHandler, LayerHandler>()
                    .AddTransient<IOrganizationHandler, OrganizationHandler>()
                    .AddTransient<IOrganizationTypeHandler, OrganizationTypeHandler>()
                    .AddTransient<IUserHandler, UserHandler>()
                    .AddTransient<IOTPHandler, OTPHandler>()
                    .AddTransient<IUserHSMAccountHandler, UserHSMAccountHandler>()
                    .AddTransient<IEmailAccountHandler, EmailAccountHandler>()
                    .AddTransient<ISendSMSHandler, SendSMSHandler>()
                    .AddTransient<IWorkflowHandler, WorkflowHandler>()
                    .AddTransient<IWorkflowStateHandler, WorkflowStateHandler>()
                    .AddTransient<INotifyConfigHandler, NotifyConfigHandler>()
                    .AddTransient<IQueueSendEmailHandler, QueueSendEmailHandler>()
                    .AddTransient<ICountryHandler, CountryHandler>()
                    .AddTransient<IProvinceHandler, ProvinceHandler>()
                    .AddTransient<IDistrictHandler, DistrictHandler>()
                    .AddTransient<IWardHandler, WardHandler>()
                    .AddTransient<IPositionHandler, PositionHandler>()
                    .AddSingleton<INotifyHandler, NotifyHandler>()
                    .AddTransient<ISystemNotifyHandler, SystemNotifyHandler>()
                .BuildServiceProvider();

                var signHashHandler = serviceProvider.GetService<ISignHashHandler>();
                signHashHandler.SendDocumentEverifyToQueue();
                
                System.Console.WriteLine("eVerify complete");
            }
            catch (Exception ex)
            {
                Log.Error("Job Error: " + ex.Message);
            }

            System.Threading.Thread.Sleep(5000);
        }
    }
}