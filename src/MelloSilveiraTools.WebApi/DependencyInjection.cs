using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Add;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Update;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using MelloSilveiraTools.WebApi.Infrastructure.ResiliencePipelines;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using System.Reflection;

namespace MelloSilveiraTools.WebApi;

/// <summary>
/// Provides extension methods to register services from the MelloSilveiraTools.WebApi package.
/// </summary>
public static class WebApiDependencyInjection
{
    /// <summary>
    /// Registers Web API services: API service agent resilience pipeline.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="resiliencePipelineSettings">Settings that parameterize the resilience pipelines.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddWebApiServices(this IServiceCollection services,
        ResiliencePipelineSettings resiliencePipelineSettings)
        => services
            .AddSingleton(provider => new ApiServiceAgentResiliencePipeline(provider.GetRequiredService<ILogger<ApiServiceAgentResiliencePipeline>>(), resiliencePipelineSettings))
            // Register generic CRUD operations as open generics so any TEntity / TFilter pair resolves
            // through DI without per-entity registrations. Both CrudController<TEntity, TFilter> and
            // the MapCrud<TEntity, TFilter> minimal-API extension consume them.
            .AddScoped(typeof(AddEntity<>))
            .AddScoped(typeof(ReadEntityById<>))
            .AddScoped(typeof(ReadEntityPaged<,>))
            .AddScoped(typeof(UpdateEntity<>))
            .AddScoped(typeof(DeleteEntity<>));

    /// <summary>
    /// Registers the authentication for AdmMaster users using JWT.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="jwtSettings">Settings used to build and validate JWT tokens.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddJweAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
    {
        services
            .AddSingleton(jwtSettings)
            .AddScoped<IAuthenticationTokenService, AuthenticationJweTokenService>()
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = AuthenticationJweTokenService.BuildTokenValidationParameters(jwtSettings);
            });

        return services;
    }

    /// <summary>
    /// Configures the documentation file for Swagger User Interface using bearer authentication.
    /// </summary>
    public static IServiceCollection AddSwaggerWithBearerSecurity(this IServiceCollection services)
    {
        (string assemblyTitle, string assemblyDescription, string assemblyLocation) = GetAssemblyAttributes();
        return services
            .AddSwaggerGen(options =>
            {
                options.SwaggerDoc(assemblyTitle, new OpenApiInfo
                {
                    Title = assemblyTitle,
                    Description = assemblyDescription,
                    Version = "v1"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Please enter into your token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                });

                options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
                {
                    { new OpenApiSecuritySchemeReference("Bearer"), [] }
                });

                string[] xmlFiles = Directory.GetFiles(assemblyLocation, "*.xml");
                foreach (string xmlFile in xmlFiles)
                {
                    options.IncludeXmlComments(xmlFile);
                }
            })
            .AddSwaggerGenNewtonsoftSupport();
    }

    /// <summary>
    /// Adds Swagger documentations to ApplicationBuilder.
    /// </summary>
    public static IApplicationBuilder UseSwaggerDocs(this IApplicationBuilder app)
    {
        (string assemblyTitle, _, _) = GetAssemblyAttributes();
        return app
            .UseSwagger()
            .UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{assemblyTitle}/swagger.json", $"{assemblyTitle} API");
                c.EnableValidator(null);
            });
    }

    private static (string Title, string Description, string Location) GetAssemblyAttributes()
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        // AppContext.BaseDirectory is preferred over Assembly.Location which returns an empty string for
        // assemblies embedded in single-file (and AOT-published) apps. Falls back to Assembly.Location for
        // edge cases where the assembly is loaded from a side-loaded path.
        string location = !string.IsNullOrEmpty(callingAssembly.Location) ? Path.GetDirectoryName(callingAssembly.Location)! : AppContext.BaseDirectory;
        return (
            callingAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? callingAssembly.GetName().Name ?? "API",
            callingAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty,
            location);
    }
}
