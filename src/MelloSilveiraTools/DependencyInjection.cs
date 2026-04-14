using MelloSilveiraTools.Authentication;
using MelloSilveiraTools.Authentication.Services;
using MelloSilveiraTools.Domain.NumericalMethods.DifferentialEquation;
using MelloSilveiraTools.Domain.Repositories;
using MelloSilveiraTools.Infrastructure.Database.Repositories;
using MelloSilveiraTools.Infrastructure.Database.Settings;
using MelloSilveiraTools.Infrastructure.Database.Sql.Provider;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.ResiliencePipelines;
using MelloSilveiraTools.Infrastructure.Services.Encryption;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
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
    /// <param name="services"></param>
    /// <param name="databaseSettings"></param>
    /// <param name="encryptionSettings"></param>
    /// <param name="resiliencePipelineSettings"></param>
    /// <returns></returns>
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
    /// Register numerical methods.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
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
    /// <param name="services"></param>
    /// <param name="jwtSettings"></param>
    /// <returns></returns>
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
    public static IServiceCollection AddSwaggerDocsWithJwtAuthentication(this IServiceCollection services, Assembly callingAssembly)
    {
        string assemblyTitle = callingAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            ?? callingAssembly.GetName().Name
            ?? "API";
        string assemblyDescription = callingAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty;
        string assemblyLocation = Path.GetDirectoryName(callingAssembly.Location)!;

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
        string assemblyTitle = Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            ?? Assembly.GetCallingAssembly().GetName().Name
            ?? "API";

        return app
            .UseSwagger()
            .UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{assemblyTitle}/swagger.json", $"{assemblyTitle} API");
                c.EnableValidator(null);
            });
    }
}