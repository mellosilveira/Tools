using MelloSilveiraTools.Authentication;
using MelloSilveiraTools.Authentication.Services;
using MelloSilveiraTools.Domain.NumericalMethods.DifferentialEquation;
using MelloSilveiraTools.Infrastructure.Database.Repositories;
using MelloSilveiraTools.Infrastructure.Database.Settings;
using MelloSilveiraTools.Infrastructure.Database.Sql.Provider;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.ResiliencePipelines;
using MelloSilveiraTools.Infrastructure.Services.Encryption;
using MelloSilveiraTools.MechanicsOfMaterials.ConstitutiveEquations;
using MelloSilveiraTools.MechanicsOfMaterials.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.GeometricProperties;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
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
    public static IServiceCollection AddToolsServices(this IServiceCollection services, 
        DatabaseSettings databaseSettings,
        EncryptionSettings encryptionSettings,
        ResiliencePipelineSettings resiliencePipelineSettings)
    {
        return services
            // Register settings.
            .AddSingleton(databaseSettings)
            .AddSingleton(encryptionSettings)
            .AddSingleton(resiliencePipelineSettings)
            // Register resilience pipelines.
            .AddSingleton<ApiServiceAgentResiliencePipeline>()
            .AddSingleton<PostgresResiliencePipeline>()
            // Register SQL providers.
            .AddSingleton<ISqlProvider, PostgresSqlProvider>()
            // Register repositories.
            .AddSingleton<IDatabaseRepository, PostgresRepository>()
            // Register logger.
            .AddSingleton<ILogger, LocalFileLogger>()
            // Register services.
            .AddScoped< IEncryptionService, EncryptionService>();
    }

    public static IServiceCollection AddMechanicalOfMaterialsServices(this IServiceCollection services)
    {
        // Register constitutive equations.
        services.AddScoped<IFatigueCalculator, FatigueCalculator>();
        services.AddScoped<IConstitutiveEquationsCalculator, MechanicsOfMaterials>();

        // Register geometric properties.
        services.AddScoped<ICircularProfileGeometricProperty, CircularProfileGeometricPropertyCalculator>();
        services.AddScoped<IRectangularProfileGeometricProperty, RectangularProfileGeometricPropertyCalculator>();

        // Register numerical methods.
        services.AddScoped<INewmarkMethod, NewmarkMethod>();
        services.AddScoped<INewmarkBetaMethod, NewmarkBetaMethod>();

        // Register factories.
        services.AddScoped<IDifferentialEquationMethodFactory, DifferentialEquationMethodFactory>(
            provider => new DifferentialEquationMethodFactory(new Dictionary<DifferentialEquationMethodEnum, IDifferentialEquationMethod>
            {
                    { DifferentialEquationMethodEnum.Newmark, provider.GetRequiredService<INewmarkMethod>() },
                    { DifferentialEquationMethodEnum.NewmarkBeta, provider.GetRequiredService<INewmarkBetaMethod>() },
            }));

        return services;

        return services;
    }

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
    public static IServiceCollection AddSwaggerDocsWithJwtAuthentication(this IServiceCollection services)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string assemblyTitle = assembly.GetCustomAttribute<AssemblyTitleAttribute>()!.Title;
        string assemblyDescription = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()!.Description;
        string assemblyLocation = Path.GetDirectoryName(assembly.Location)!;

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

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                        new List<string>()
                    }
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
        string assemblyTitle = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()!.Title;

        return app
            .UseSwagger()
            .UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{assemblyTitle}/swagger.json", $"{assemblyTitle} API");
                c.EnableValidator(null);
            });
    }
}