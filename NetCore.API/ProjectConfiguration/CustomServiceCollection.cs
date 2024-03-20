using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using NetCore.Business;
using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NetCore.API
{
    public static class CustomServiceCollection
    {
        public static IServiceCollection RegisterCacheComponents(this IServiceCollection services)
        {
            if (Utils.GetConfig("redis:enabled") == "true")
            {
                services.AddSingleton<ICacheService, RedisCacheService>();
            }
            else
            {
                services.AddSingleton<ICacheService, InMemoryCacheService>();
            }
            return services;
        }

        public static IServiceCollection RegisterCustomerModelParsingServiceComponents(this IServiceCollection services, IConfiguration configuration)
        {
            //Custom model parsing
            services.AddControllers().ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    string messages = "";
                    try
                    {
                        messages = string.Join("; ", context.ModelState.Values
                                .SelectMany(x => x.Errors)
                                .Select(x => x.ErrorMessage));
                    }
                    catch (Exception ex)
                    { messages = ex.Message; }
                    return Helper.TransformData(new ResponseError(Code.BadRequest, Helper.ErrorMessage_IncorrectInput + ": " + messages));
                };
            });

            return services;
        }


        public static IServiceCollection RegisterAPIVersionServiceComponents(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApiVersioning(
                 options =>
                 {
                     options.AssumeDefaultVersionWhenUnspecified = true;
                     options.ReportApiVersions = true;
                 });
            services.AddVersionedApiExplorer(x =>
            {
                x.GroupNameFormat = "'v'VVV";
                // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                x.SubstituteApiVersionInUrl = true;
            });

            return services;
        }

        public static IServiceCollection AddCustomAuthenServiceComponents(this IServiceCollection services, IConfiguration configuration)
        {
            // Authorize related configuration
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Custom Scheme";
                options.DefaultChallengeScheme = "Custom Scheme";
            }).AddCustomAuth(o => { });

            return services;
        }
    }
}