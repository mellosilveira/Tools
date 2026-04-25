using MelloSilveiraTools.Application.Operations.Plugins.Cache;
using MelloSilveiraTools.Application.Operations.Plugins.Get;
using MelloSilveiraTools.Application.Operations.Plugins.Load;
using MelloSilveiraTools.Application.Operations.Plugins.Reload;
using MelloSilveiraTools.Authentication;
using MelloSilveiraTools.Authentication.Services;
using MelloSilveiraTools.Domain.NumericalMethods.DifferentialEquation;
using MelloSilveiraTools.Domain.Repositories;
using MelloSilveiraTools.Infrastructure.Caching;
using MelloSilveiraTools.Infrastructure.Database.Repositories;
using MelloSilveiraTools.Infrastructure.Database.Settings;
using MelloSilveiraTools.Infrastructure.Database.Sql.Provider;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.Plugins;
using MelloSilveiraTools.Infrastructure.Plugins.Persistences;
using MelloSilveiraTools.Infrastructure.Providers;
using MelloSilveiraTools.Infrastructure.ResiliencePipelines;
using MelloSilveiraTools.Infrastructure.Services.Encryption;
using MelloSilveiraTools.Infrastructure.Services.Plugins;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Reflection;

namespace MelloSilveiraTools;

/// <summary>
/// Provides extension methods to dependency injection of Tools project.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the services of Tools project.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="databaseSettings">Database connection and behavior settings.</param>
    /// <param name="encryptionSettings">Settings used by the encryption service.</param>
    /// <param name="resiliencePipelineSettings">Settings that parameterize the resilience pipelines.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddToolsServices(this IServiceCollection services, DatabaseSettings databaseSettings, EncryptionSettings encryptionSettings, ResiliencePipelineSettings resiliencePipelineSettings)
        => services
            // Register settings.
            .AddSingleton(databaseSettings)
            .AddSingleton(encryptionSettings)
            .AddSingleton(resiliencePipelineSettings)
            // Register resilience pipelines.
            .AddSingleton(provider => new ApiServiceAgentResiliencePipeline(provider.GetRequiredService<ILogger>(), resiliencePipelineSettings))
            .AddSingleton(provider => new PostgresResiliencePipeline(provider.GetRequiredService<ILogger>(), resiliencePipelineSettings))
            .AddSingleton(provider => new SmtpResiliencePipeline(provider.GetRequiredService<ILogger>(), resiliencePipelineSettings))
            // Register SQL providers.
            .AddSingleton<ISqlProvider, PostgresSqlProvider>()
            // Register repositories.
            .AddSingleton<IRepository, PostgresRepository>()
            // Register logger.
            .AddSingleton<ILogger, LocalFileLogger>()
            // Register services.
            .AddScoped<IEncryptionService, EncryptionService>();

    /// <summary>
    /// Registers plugin infrastructure, caching, providers, and plugin operations.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="pluginSettings">Settings that describe plugin folders and behavior.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddPluginServices(this IServiceCollection services, PluginSettings pluginSettings)
        => services
            // Register settings.
            .AddSingleton(pluginSettings)
            // Required so plugin cache operations can read the {target} route value.
            .AddHttpContextAccessor()
            // Register caching.
            .AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>()
            .AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>()
            // Register dynamic service provider (runtime plugin service registration).
            .AddSingleton(services)
            .AddSingleton<IDynamicServiceProvider, DynamicServiceProvider>()
            // Register plugin processors and cache.
            .AddSingleton<PluginCache>()
            .AddSingleton<PluginFileProcessor>()
            .AddSingleton<PluginAssemblyProcessor>()
            // Register plugin cache persistences as keyed services. The key matches
            // the {target} route segment on cache-persist/restore endpoints. Consumers
            // of this package can register additional implementations under their own
            // keys (e.g. services.AddKeyedSingleton<IPluginCachePersistence, RedisPluginCachePersistence>("redis");)
            // and the same endpoints will route to them without further changes.
            .AddKeyedSingleton<IPluginCachePersistence, JsonFilePluginCachePersistence>(PluginCacheTargets.File)
            .AddKeyedSingleton<IPluginCachePersistence, DatabasePluginCachePersistence>(PluginCacheTargets.Database)
            // Non-keyed IPluginCachePersistence resolves the correct keyed implementation
            // per request, based on the {target} segment of the current route. This is
            // what PluginService and any other consumer gets through normal constructor
            // injection.
            .AddScoped(serviceProvider =>
            {
                var accessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                object? target = accessor.HttpContext?.Request.RouteValues["target"];
                return serviceProvider.GetRequiredKeyedService<IPluginCachePersistence>(target);
            })
            // Plugin cache operations run inside an HTTP request, so the scoped
            // persistence factory above resolves to the right implementation.
            // PluginService must be scoped as well to avoid capturing a scoped
            // dependency from a wider lifetime.
            .AddScoped<IPluginService, PluginService>()
            // Register plugin operations.
            .AddScoped<GetPlugins>()
            .AddScoped<LoadPlugins>()
            .AddScoped<ReloadPlugins>()
            .AddScoped<ClearPluginCache>()
            .AddScoped<PersistPluginCache>()
            .AddScoped<RestorePluginCache>();

    /// <summary>
    /// Register numerical methods.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddNumericalMethods(this IServiceCollection services)
        => services
            // Register numerical methods.
            .AddSingleton<IDifferentialEquationMethod, NewmarkMethod>()
            .AddSingleton<IDifferentialEquationMethod, NewmarkBetaMethod>()
            // Register factories.
            .AddSingleton<DifferentialEquationMethodFactory>();

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
    /// Configures the documentation file for Swagger User Interface using JWT authentication.
    /// </summary>
    public static IServiceCollection AddSwaggerDocsWithJwtAuthentication(this IServiceCollection services)
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
        return (
            callingAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? callingAssembly.GetName().Name ?? "API",
            callingAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty,
            Path.GetDirectoryName(callingAssembly.Location)!);
    }
}