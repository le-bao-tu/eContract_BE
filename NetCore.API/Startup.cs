using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCore.Shared;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;

namespace NetCore.API
{
    public class Startup
    {
        public IConfiguration configuration { get; }
        public Startup(IWebHostEnvironment env)
        {
            var builder = StartupHelpers.CreateDefaultConfigurationBuilder(env);

            if (env.IsDevelopment())
            {
                //builder.AddUserSecrets<Startup>();
            }

            configuration = builder.Build();
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.RegisterCacheComponents();

            services.RegisterDataContextServiceComponents(configuration);

            services.AddMvcCore();

            services.AddOptions();
            services.AddCors();
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                });

            services.RegisterAPIVersionServiceComponents(configuration);

            services.AddCustomAuthenServiceComponents(configuration);

            services.RegisterSwaggerServiceComponents(configuration);

            services.RegisterCustomerModelParsingServiceComponents(configuration);

            if (Utils.GetConfig("redis:enabled") == "true")
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = Utils.GetConfig("redis:configuration");
                    options.InstanceName = Utils.GetConfig("redis:instanceName");
                });
            }

            services.AddHealthChecks();

            services.AddSignalR();

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddRouting();
            services.AddHttpClient();

            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("Users", policy => policy.RequireRole("Users"));
            //});

            //services.AddAuthentication(options =>
            //{
            //    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            //});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (Utils.GetConfig("AppSettings:EnableSwagger") == "true")
            {
                // Enable middleware to serve generated Swagger as a JSON endpoint.
                app.UseSwagger(c =>
                {
                    c.SerializeAsV2 = true;
                });

                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
                // specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Net Core API V1");
                    c.DocExpansion(DocExpansion.None);
                    // To serve SwaggerUI at application's root page, set the RoutePrefix property to an empty string.
                    c.RoutePrefix = "";
                });
            }

            app.UseSentryTracing();

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // global cors policy
            app.UseCors(x => x
               .AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());

            app.UseDefaultFiles(new DefaultFilesOptions()
            {
                DefaultFileNames = new List<string>() { "index.html" },
                RequestPath = new PathString("")
            });
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SignalRHub>("/signalr_hub");
            });

            app.UseHealthChecks("/health");//your request URL will be health

            Spire.License.LicenseProvider.SetLicenseKey(Utils.GetConfig("Spire:License"));
        }
    }
}
