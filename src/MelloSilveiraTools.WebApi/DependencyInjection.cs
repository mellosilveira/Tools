using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Add;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Update;
using MelloSilveiraTools.WebApi.Application.Commands.Health;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Create;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Download;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Read;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Upload;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Login;
using MelloSilveiraTools.WebApi.Application.Commands.Users.RefreshToken;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Register;
using MelloSilveiraTools.WebApi.Application.Commands.Users.ValidateToken;
using MelloSilveiraTools.WebApi.Application.Middlewares;
using MelloSilveiraTools.WebApi.Application.RateLimiting;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using MelloSilveiraTools.WebApi.Infrastructure.ResiliencePipelines;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.OpenApi;
using Npgsql;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace MelloSilveiraTools.WebApi;

/// <summary>
/// Provides extension methods to register services from the MelloSilveiraTools.WebApi package.
/// </summary>
public static class WebApiDependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Web API services: API service agent resilience pipeline.
        /// </summary>
        /// <param name="defaultResiliencePipelineSettings">Settings that parameterize the default resilience pipelines.</param>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddWebApiServices(ResiliencePipelineSettings defaultResiliencePipelineSettings) => services
            .AddSingleton(provider => new ApiServiceAgentResiliencePipeline(provider.GetRequiredService<ILogger<ApiServiceAgentResiliencePipeline>>(), defaultResiliencePipelineSettings))
            // Register generic CRUD operations as open generics so any TEntity / TFilter pair resolves
            // through DI without per-entity registrations. Both CrudController<TEntity, TFilter> and
            // the MapCrud<TEntity, TFilter> minimal-API extension consume them.
            .AddScoped(typeof(AddEntity<>))
            .AddScoped(typeof(DeleteEntity<>))
            .AddScoped(typeof(ReadEntityById<>))
            .AddScoped(typeof(ReadEntityPaged<,>))
            .AddScoped(typeof(UpdateEntity<>))
            .AddScoped<CheckHealthCommand>()
            .AddSingleton<IValidator<AddStorageEntryRequest>, AddStorageEntryValidator>()
            .AddSingleton<IValidator<DeleteStorageEntryRequest>, DeleteStorageEntryValidator>()
            .AddSingleton<IValidator<UpdateStorageEntryFileRequest>, UpdateStorageEntryFileValidator>()
            .AddSingleton<AddStorageEntryCommand>()
            .AddSingleton<DeleteStorageEntryCommand>()
            .AddSingleton<DownloadStorageEntryCommand>()
            .AddSingleton<ReadStorageEntriesCommand>()
            .AddSingleton<UpdateStorageEntryFileCommand>();


        /// <summary>
        /// Registers ASP.NET Core native health checks for MelloSilveiraTools Web API hosts,
        /// aggregating file storage health checks and, when <paramref name="databaseSettings"/> is provided,
        /// PostgreSQL database health checks.
        /// </summary>
        /// <param name="databaseSettings">Optional database settings; when provided, registers PostgreSQL health checks with the configured operation timeout.</param>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddWebApiHealthChecks(DatabaseSettings? databaseSettings = null)
        {
            IHealthChecksBuilder healthChecksBuilder = services
                .AddHealthChecks()
                .AddFileStorageHealthCheck();

            if (databaseSettings is not null)
                healthChecksBuilder.AddDatabaseHealthCheck(databaseSettings);

            return services;
        }

        /// <summary>
        /// Registers JWE authentication, authentication commands, and the database-backed <see cref="AuthorizationPolicies.SuperuserOnly"/> authorization policy.
        /// </summary>
        /// <param name="jwtSettings">Settings used to build and validate JWT tokens.</param>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddJweAuthentication(JwtSettings jwtSettings)
        {
            services
                .AddSingleton(jwtSettings)
                .AddSingleton<IAuthorizationHandler, SuperuserAuthorizationHandler>()
                .AddScoped<IAuthenticationTokenService, AuthenticationJweTokenService>()
                .AddScoped<LoginCommand>()
                .AddScoped<RefreshAuthenticationTokenCommand>()
                .AddScoped<RegisterUserCommand>()
                .AddScoped<ValidateAuthenticationTokenCommand>()
                .AddScoped<IValidator<RegisterUserRequest>, RegisterUserValidator>()
                .AddScoped<IValidator<LoginRequest>, LoginValidator>()
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

            services
                .AddAuthorizationBuilder()
                .AddPolicy(AuthorizationPolicies.SuperuserOnly, policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new SuperuserRequirement()));

            return services;
        }

        /// <summary>
        /// Configures the documentation file for Swagger User Interface using bearer authentication.
        /// </summary>
        public IServiceCollection AddSwaggerWithBearerSecurity() => services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(options =>
            {
                (string assemblyTitle, string assemblyDescription, string assemblyLocation) = GetAssemblyAttributes();

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

        /// <summary>
        /// Registers the default fixed-window ASP.NET Core rate-limiting policies defined in <see cref="RateLimitPolicies"/>:
        /// <see cref="RateLimitPolicies.Auth"/>, <see cref="RateLimitPolicies.AnonymousCompute"/>, and
        /// <see cref="RateLimitPolicies.PluginUpload"/> (returning <c>429 Too Many Requests</c> when exceeded).
        /// </summary>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddDefaultRateLimiting() => services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
            {
                string remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                bool isRegistration = httpContext.Request.Path.Value?.TrimEnd('/').EndsWith("/register", StringComparison.OrdinalIgnoreCase) == true;
                string partitionKey = isRegistration ? $"{remoteIp}:register" : $"{remoteIp}:auth";
                int permitLimit = isRegistration ? 5 : 20;

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            options.AddPolicy(RateLimitPolicies.AnonymousCompute, httpContext =>
            {
                if (httpContext.User.Identity?.IsAuthenticated == true)
                    return RateLimitPartition.GetNoLimiter("authenticated");

                string remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter($"{remoteIp}:anon-compute", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            options.AddPolicy(RateLimitPolicies.PluginUpload, httpContext =>
            {
                long? currentUserId = httpContext.RequestServices.GetService<ICurrentUserProvider>()?.GetCurrentUserId();
                string? claimUserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.Identity?.Name;
                string userKey = currentUserId.HasValue
                    ? $"user:{currentUserId.Value}"
                    : httpContext.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(claimUserId)
                        ? $"user:{claimUserId}"
                        : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetFixedWindowLimiter($"{userKey}:plugin-upload", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });
        });

        /// <summary>
        /// Configures the default ASP.NET Core CORS policy using explicit <paramref name="allowedOrigins"/>,
        /// falling back to <c>AllowAnyOrigin()</c> only when <paramref name="allowedOrigins"/> is empty and
        /// <paramref name="allowAnyOriginWhenEmpty"/> is <see langword="true"/> (typically in Development).
        /// </summary>
        /// <param name="allowedOrigins">Explicitly allowed origins; when empty, requests are allowed from any origin only if <paramref name="allowAnyOriginWhenEmpty"/> is <see langword="true"/>.</param>
        /// <param name="allowAnyOriginWhenEmpty">When <see langword="true"/> and no origins are specified, permits any origin.</param>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddDefaultCors(string[]? allowedOrigins, bool allowAnyOriginWhenEmpty = false)
            => services.AddCors(options => options.AddDefaultPolicy(policy =>
            {
                if (allowedOrigins is { Length: > 0 })
                {
                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
                }
                else if (allowAnyOriginWhenEmpty)
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
            }));

        /// <summary>
        /// Configures ASP.NET Core minimal API HTTP JSON serialization options required by MelloSilveiraTools
        /// polymorphic contracts (<see cref="System.Text.Json.JsonSerializerOptions.AllowOutOfOrderMetadataProperties"/>)
        /// and string-based enum serialization (<see cref="JsonStringEnumConverter"/>).
        /// </summary>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection ConfigureToolsHttpJsonOptions() => services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    }

    /// <summary>
    /// Adds Swagger documentations to ApplicationBuilder.
    /// </summary>
    public static IApplicationBuilder UseSwaggerDocs(this IApplicationBuilder app) => app
        .UseSwagger()
        .UseSwaggerUI(c =>
        {
            (string assemblyTitle, _, _) = GetAssemblyAttributes();
            c.SwaggerEndpoint($"/swagger/{Uri.EscapeDataString(assemblyTitle)}/swagger.json", $"{assemblyTitle} API");
            c.EnableValidator(null);
        });

    /// <summary>
    /// Registers custom HTTP exception handling middleware.
    /// </summary>
    public static IApplicationBuilder UseCustomMiddlewares(this IApplicationBuilder app) => app.UseMiddleware<ExceptionHandlingHttpMiddleware>();

    /// <summary>
    /// Configures Kestrel <c>MaxRequestBodySize</c> and ASP.NET Core multipart <see cref="FormOptions.MultipartBodyLengthLimit"/>
    /// consistently to <paramref name="maxRequestBodySizeBytes"/>.
    /// </summary>
    /// <param name="builder">Web application builder to configure.</param>
    /// <param name="maxRequestBodySizeBytes">Maximum permitted request and multipart body size in bytes (must be positive).</param>
    /// <returns>The same <paramref name="builder"/> instance to allow call chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxRequestBodySizeBytes"/> is less than or equal to zero.</exception>
    public static WebApplicationBuilder ConfigureRequestBodyLimits(this WebApplicationBuilder builder, long maxRequestBodySizeBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRequestBodySizeBytes);

        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxRequestBodySizeBytes);
        builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxRequestBodySizeBytes);
        return builder;
    }

    /// <summary>
    /// Registers the mandatory ordered security middleware pipeline:
    /// <c>UseAuthentication()</c> → <c>UseUserContext()</c> → <c>UseAuthorization()</c> (and optionally <c>UseRateLimiter()</c>).
    /// Preserving this sequence guarantees that database-backed user/superuser resolution runs before authorization
    /// and that authenticated callers bypass anonymous rate limits.
    /// </summary>
    /// <param name="app">Application builder.</param>
    /// <param name="useRateLimiter">When <see langword="true"/> (default), appends <c>UseRateLimiter()</c> after <c>UseAuthorization()</c>.</param>
    /// <returns>The same <paramref name="app"/> instance for chaining.</returns>
    public static IApplicationBuilder UseWebApiSecurityPipeline(this IApplicationBuilder app, bool useRateLimiter = true)
    {
        app.UseAuthentication();
        app.UseUserContext();
        app.UseAuthorization();

        if (useRateLimiter)
            app.UseRateLimiter();

        return app;
    }

    /// <summary>
    /// Verifies eagerly at startup that the configured PostgreSQL connection role does not possess
    /// <c>SUPERUSER</c> or <c>BYPASSRLS</c> privileges unless <see cref="DatabaseSettings.AllowSuperuserDatabaseConnection"/>
    /// is explicitly set to <see langword="true"/>. Transient connectivity errors are ignored only in Development.
    /// </summary>
    /// <param name="app">Built web application whose DI container holds <see cref="NpgsqlDataSource"/> and <see cref="DatabaseSettings"/>.</param>
    /// <param name="cancellationToken">Token used to cancel the startup verification query.</param>
    public static Task EnsureDatabaseRlsSafetyAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        DatabaseSettings? databaseSettings = app.Services.GetService<DatabaseSettings>();
        bool allowSuperuser = databaseSettings?.AllowSuperuserDatabaseConnection ?? false;
        return app.EnsureDatabaseRlsSafetyAsync(allowSuperuser, cancellationToken);
    }

    /// <summary>
    /// Verifies eagerly at startup that the configured PostgreSQL connection role does not possess
    /// <c>SUPERUSER</c> or <c>BYPASSRLS</c> privileges unless <paramref name="allowSuperuserDatabaseConnection"/>
    /// is <see langword="true"/>. Transient connectivity errors are ignored only in Development.
    /// </summary>
    /// <param name="app">Built web application whose DI container holds <see cref="NpgsqlDataSource"/>.</param>
    /// <param name="allowSuperuserDatabaseConnection">Explicit opt-out permitting superuser/bypassrls roles.</param>
    /// <param name="cancellationToken">Token used to cancel the startup verification query.</param>
    public static Task EnsureDatabaseRlsSafetyAsync(this WebApplication app, bool allowSuperuserDatabaseConnection, CancellationToken cancellationToken = default)
    {
        NpgsqlDataSource dataSource = app.Services.GetRequiredService<NpgsqlDataSource>();
        return dataSource.EnsureNonSuperuserRoleAsync(
            app.Logger,
            allowSuperuser: allowSuperuserDatabaseConnection,
            ignoreConnectionErrors: app.Environment.IsDevelopment(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Registers <see cref="UserContextMiddleware"/> to propagate the authenticated user's identifier into
    /// <see cref="ICurrentUserProvider"/> for PostgreSQL Row-Level Security (RLS) and background pipelines.
    /// Must be invoked after <c>UseAuthentication()</c>.
    /// </summary>
    /// <param name="app">Application builder.</param>
    /// <returns>The same <paramref name="app"/> instance for chaining.</returns>
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder app) => app.UseMiddleware<UserContextMiddleware>();

    private static (string Title, string Description, string Location) GetAssemblyAttributes()
    {
        Assembly callingAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
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
