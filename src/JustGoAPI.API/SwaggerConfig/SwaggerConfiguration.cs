using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace JustGoAPI.API.SwaggerConfig
{
    public static class SwaggerConfiguration
    {
        public static void AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen(swagger =>
            {  
                swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter JWT token in the text input below"
                });

                swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference=new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },Array.Empty<string>()
                    }
                });

                swagger.AddSecurityDefinition("RefreshToken", new OpenApiSecurityScheme
                {
                    Name = "X-Refresh-Token",
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "Random",
                    In = ParameterLocation.Header,
                    Description = "Enter your Refresh Token"
                });

                swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference=new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="RefreshToken"
                            }
                        },Array.Empty<string>()
                    }
                });
                swagger.AddSecurityDefinition("TenantId", new OpenApiSecurityScheme
                {
                    Name = "X-Tenant-Id",
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "Random",
                    In = ParameterLocation.Header,
                    Description = "Enter Tenant Id"
                });

                swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference=new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="TenantId"
                            }
                        },Array.Empty<string>()
                    }
                });
            });
        }
        public static void UseSwaggerDocumentation(this IApplicationBuilder app)
        {
            var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                        $"JustGoAPI {description.GroupName.ToUpperInvariant()}");
                }

                options.RoutePrefix = string.Empty;
                options.DefaultModelsExpandDepth(-1);
                options.DocumentTitle = "JustGoAPI Documentation";
                options.DocExpansion(DocExpansion.None);
                options.InjectJavascript("/Scripts/swagger-auth-persist.js");
                options.InjectJavascript("/scripts/swagger-hierarchy-plugin.js");
                options.InjectJavascript("/scripts/swagger-hierarchy-shim.js");
                options.InjectJavascript("/scripts/reinit-swagger-with-hierarchy.js");
            });
        }
    }
}
