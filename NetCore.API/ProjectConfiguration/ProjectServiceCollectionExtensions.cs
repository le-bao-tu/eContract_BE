using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetCore.Business;
using NetCore.Data;
using NetCore.DataLog;
using NetCore.Shared;
namespace NetCore.API
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods add project services.
    /// </summary>
    /// <remarks>
    /// AddSingleton - Only one instance is ever created and returned.
    /// AddScoped - A new instance is created and returned for each request/response cycle.
    /// AddTransient - A new instance is created and returned each time.
    /// </remarks>
    public static class ProjectServiceCollectionExtensions
    {
        public static IServiceCollection RegisterDataContextServiceComponents(this IServiceCollection services, IConfiguration configuration)
        {
            #region Config database
            var isTesting = (Utils.GetConfig("ConnectionString:IsTesting") == "HAS_TEST");
            var databaseType = Utils.GetConfig("ConnectionString:DbType");
            var connectionString = Utils.GetConfig("ConnectionString:" + databaseType);

            if (isTesting)
            {
                services.AddDbContext<DataContext>(x => x.UseInMemoryDatabase(connectionString));
            }
            else
            {
                switch (databaseType)
                {
                    case "MySqlPomeloDatabase":
                        services.AddDbContext<DataContext>(x => x.UseMySql(connectionString));
                        break;

                    case "MSSQLDatabase":
                        services.AddDbContext<DataContext>(x => x.UseSqlServer(connectionString));
                        break;

                    case "Sqlite":
                        services.AddDbContext<DataContext>(x => x.UseSqlite(connectionString));
                        break;
                    case "PostgreSQLDatabase":
                        services.AddDbContext<DataContext>(x => x.UseNpgsql(connectionString));
                        break;

                    default:
                        services.AddDbContext<DataContext>(x => x.UseSqlServer(connectionString));
                        break;
                }
            }

            #endregion Config database

            #region MongoDB

            // requires using Microsoft.Extensions.Options
            // SystemLog
            services.Configure<MongoDBDatabaseSettings>(
                configuration.GetSection(nameof(MongoDBDatabaseSettings)));

            services.AddSingleton<IMongoDBDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<MongoDBDatabaseSettings>>().Value);

            services.AddTransient<ISystemLogHandler, SystemLogHandler>();
            #endregion

            // Core
            services.AddTransient<IEmailHandler, EmailHandler>();
            services.AddTransient<ICallStoreHelper, CallStoreHelper>();
            services.AddTransient<ILayerHandler, LayerHandler>();

            // Business
            services.AddTransient<ISystemApplicationHandler, SystemApplicationHandler>();
            services.AddTransient<IActiveDirectoryHandler, ActiveDirectoryHandler>();

            #region eContract
            services.AddTransient<ISeedDataHandler, SeedDataHandler>();
            services.AddTransient<IDocumentTypeHandler, DocumentTypeHandler>();
            services.AddTransient<IMetaDataHandler, MetaDataHandler>();
            services.AddTransient<IDocumentTemplateHandler, DocumentTemplateHandler>();
            services.AddTransient<IDashboardHandler, DashboardHandler>();
            services.AddTransient<IDocumentBatchHandler, DocumentBatchHandler>();
            services.AddTransient<IDocumentHandler, DocumentHandler>();
            services.AddTransient<ISignServiceHandler, SignServiceHandler>();
            services.AddTransient<ISignHashHandler, SignHashHandler>();
            services.AddTransient<ISignDocumentHandler, SignDocumentHandler>();
            services.AddTransient<IOTPHandler, OTPHandler>();
            services.AddTransient<INotifyHandler, NotifyHandler>();
            services.AddTransient<IUserRoleHandler, UserRoleHandler>();
            services.AddTransient<IOrganizationConfigHandler, OrganizationConfigHandler>();
            services.AddTransient<IUserSignConfigHandler, UserSignConfigHandler>();
            #endregion

            #region User&Org
            services.AddTransient<IOrganizationHandler, OrganizationHandler>();
            services.AddTransient<IOrganizationTypeHandler, OrganizationTypeHandler>();
            services.AddTransient<IUserHandler, UserHandler>();
            services.AddTransient<IOTPHandler, OTPHandler>();
            services.AddTransient<IUserHSMAccountHandler, UserHSMAccountHandler>();
            #endregion

            #region Email
            services.AddTransient<IEmailAccountHandler, EmailAccountHandler>();
            services.AddTransient<IQueueSendEmailHandler, QueueSendEmailHandler>();
            #endregion

            #region
            services.AddTransient<ISendSMSHandler, SendSMSHandler>();
            #endregion

            #region Workflow
            services.AddTransient<IWorkflowHandler, WorkflowHandler>();
            services.AddTransient<IWorkflowStateHandler, WorkflowStateHandler>();
            services.AddTransient<INotifyConfigHandler, NotifyConfigHandler>();
            #endregion

            #region Catalog
            services.AddTransient<ICountryHandler, CountryHandler>();
            services.AddTransient<IProvinceHandler, ProvinceHandler>();
            services.AddTransient<IDistrictHandler, DistrictHandler>();
            services.AddTransient<IWardHandler, WardHandler>();
            services.AddTransient<IPositionHandler, PositionHandler>();
            #endregion

            #region Permission
            services.AddTransient<IRoleHandler, RoleHandler>();
            services.AddTransient<INavigationHandler, NavigationHandler>();
            services.AddTransient<IRightHandler, RightHandler>();
            #endregion

            services.AddTransient<IADSSCoreHandler, ADSSCoreHandler>();

            services.AddTransient<ITestServiceHandler, TestServiceHandler>();
            services.AddTransient<ISystemNotifyHandler, SystemNotifyHandler>();

            return services;
        }
    }
}