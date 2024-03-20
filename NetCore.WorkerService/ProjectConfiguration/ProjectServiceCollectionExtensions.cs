using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetCore.Business;
using NetCore.Data;
using NetCore.DataLog;
using NetCore.Shared;

namespace NetCore.WorkerService
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
            #endregion

            #region NotifyConfig
            //services.AddTransient<ISendSMSHandler, SendSMSHandler>();
            services.AddTransient<IUserRoleHandler, UserRoleHandler>();
            services.AddTransient<IOrganizationHandler, OrganizationHandler>();
            services.AddScoped<ICacheService, InMemoryCacheService>();
            services.AddTransient<IOrganizationConfigHandler, OrganizationConfigHandler>();
            services.AddTransient<INotifyHandler, NotifyHandler>();
            services.AddTransient<INotifyConfig, NotifyConfig>();
            services.AddScoped<IEmailHandler, EmailHandler>();
            #endregion

            return services;
        }
    }
}