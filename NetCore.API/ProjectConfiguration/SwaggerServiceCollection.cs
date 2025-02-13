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
using System.Reflection;

namespace NetCore.API
{
    public static class SwaggerServiceCollection
    {
        public static IServiceCollection RegisterSwaggerServiceComponents(this IServiceCollection services, IConfiguration configuration)
        {
            // Register the Swagger Generator service. This service is responsible for genrating Swagger Documents.
            // Note: Add this service at the end after AddMvc() or AddMvcCore().
            //Locate the XML file being generated by ASP.NET...
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine($"{AppContext.BaseDirectory}", xmlFile);
            services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = Utils.GetConfig("AppSettings:Tittle"),
                    Version = "v1",
                    Description = Utils.GetConfig("AppSettings:Description"),
                    TermsOfService = string.IsNullOrEmpty(Utils.GetConfig("AppSettings:TermsOfService")) ? null : new Uri(Utils.GetConfig("AppSettings:TermsOfService")),
                    Contact = new OpenApiContact
                    {
                        Name = Utils.GetConfig("AppSettings:Contact:Name"),
                        Email = Utils.GetConfig("AppSettings:Contact:Email"),
                        Url = string.IsNullOrEmpty(Utils.GetConfig("AppSettings:Contact:Url")) ? null : new Uri(Utils.GetConfig("AppSettings:Contact:Url")),
                    },
                    License = new OpenApiLicense
                    {
                        Name = Utils.GetConfig("AppSettings:License:Name"),
                        Url = string.IsNullOrEmpty(Utils.GetConfig("AppSettings:License:Url")) ? null : new Uri(Utils.GetConfig("AppSettings:License:Url")),
                    }
                });

                o.IncludeXmlComments(xmlPath);

                o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Authorization: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                o.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id= "Bearer"
                            },
                            Scheme ="JWT",
                            Name= "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });

                o.TagActionsBy(api =>
                {
                    if (api.GroupName != null)
                    {
                        return new[] { api.GroupName };
                    }

                    var controllerActionDescriptor = api.ActionDescriptor as ControllerActionDescriptor;
                    if (controllerActionDescriptor != null)
                    {
                        return new[] { controllerActionDescriptor.ControllerName };
                    }

                    throw new InvalidOperationException("Unable to determine tag for endpoint.");
                });

                o.DocInclusionPredicate((name, api) => true);
            });

            return services;
        }
    }
}