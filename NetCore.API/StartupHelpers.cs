using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace NetCore.API
{
    public class StartupHelpers
    {
        public static IConfigurationBuilder CreateDefaultConfigurationBuilder(IWebHostEnvironment env)
        {
            return new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
        }
    }
}
