using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Logging;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Email;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.WebApi.Authentication;
using System.Text;

namespace MelloSilveiraTools.WebApi.Host;

/// <summary>
/// Credentials for bootstrapping the application superuser account on startup.
/// </summary>
public sealed record SuperuserSettings
{
    /// <summary>
    /// Gets the unique username for the application superuser.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the plaintext password secret for bootstrapping the superuser (stored hashed via PBKDF2 in PostgreSQL).
    /// </summary>
    public required string Password { get; init; }
}

/// <summary>
/// Root strongly-typed configuration settings for <c>MelloSilveiraTools.WebApi.Host</c>,
/// aggregating every required and optional settings contract needed to bootstrap the application.
/// </summary>
public sealed record Settings
{
    /// <summary>
    /// Gets the default Polly v8 resilience pipeline settings.
    /// </summary>
    public required ResiliencePipelineSettings DefaultResiliencePipeline { get; init; }

    /// <summary>
    /// Gets the PostgreSQL database connection and timeout settings.
    /// </summary>
    public required DatabaseSettings Database { get; init; }

    /// <summary>
    /// Gets the PBKDF2 password hashing and encryption settings.
    /// </summary>
    public required EncryptionSettings Encryption { get; init; }

    /// <summary>
    /// Gets the file storage provider settings (Local, DFS, BlobStorage, AwsS3).
    /// </summary>
    public required FileStorageSettings FileStorage { get; init; }

    /// <summary>
    /// Gets the curve fitter engine and deterministic seed settings.
    /// </summary>
    public required CurveFitterSettings CurveFitter { get; init; }

    /// <summary>
    /// Gets the experimental data processing pipeline topology and upload limit settings.
    /// </summary>
    public required ExperimentalDataSettings ExperimentalData { get; init; }

    /// <summary>
    /// Gets the parametric sensitivity sweep pipeline settings.
    /// </summary>
    public required ParametricSweepSettings ParametricSweep { get; init; }

    /// <summary>
    /// Gets the real-time pipeline progress notifier channel settings.
    /// </summary>
    public required PipelineProgressNotifierSettings PipelineProgressNotifier { get; init; }

    /// <summary>
    /// Gets the JWE / JWT authentication settings.
    /// </summary>
    public required JwtSettings Jwt { get; init; }

    /// <summary>
    /// Gets the superuser account credentials seeded on startup.
    /// </summary>
    public required SuperuserSettings Superuser { get; init; }

    /// <summary>
    /// Gets whether mechanical constitutive model calculators should be registered in DI.
    /// </summary>
    public required bool AddMechanicalModels { get; init; }

    /// <summary>
    /// Gets whether bundled sample CSV datasets should be uploaded via <c>AddStorageEntryCommand</c> during application startup.
    /// </summary>
    public required bool UploadSampleDatasetsOnStartup { get; init; }

    /// <summary>
    /// Gets the allowed CORS origins. When empty in Development, all origins are permitted;
    /// in non-Development environments, only explicitly listed origins are allowed.
    /// </summary>
    public string[] AllowedCorsOrigins { get; init; } = [];

    /// <summary>
    /// Gets the dynamic plugin runtime settings.
    /// </summary>
    public required PluginSettings Plugins { get; init; }

    /// <summary>
    /// Gets optional SMTP email service settings.
    /// </summary>
    public EmailSettings? Email { get; init; }

    /// <summary>
    /// Gets optional Serilog multi-sink logger settings.
    /// </summary>
    public LoggerSettings? Logger { get; init; }

    private static readonly HashSet<string> KnownPlaceholderSecrets = new(StringComparer.Ordinal)
    {
        "MelloSilveiraToolsQuickstartKey!",
        "MelloSilveiraToolsSigningKey2026!!",
        "SuperuserQuickstart2026!"
    };

    /// <summary>
    /// Validates critical runtime prerequisites eagerly before building the application container.
    /// </summary>
    /// <param name="isDevelopment">When <see langword="false"/>, rejects known quickstart placeholder secrets.</param>
    /// <returns>A <see cref="Result"/> indicating whether all required settings are valid, or listing every validation failure.</returns>
    public Result Validate(bool isDevelopment = true) => Result.CreateSuccessOk()
        .AddErrorIfNullOrWhiteSpace(Database.ConnectionString, "Configuration secret 'Database:ConnectionString' is required (configure via User Secrets or environment variable 'Database__ConnectionString').")
        .AddErrorIfNullOrWhiteSpace(Jwt.Issuer, "Configuration key 'Jwt:Issuer' is required.")
        .AddErrorIfNullOrWhiteSpace(Jwt.Audience, "Configuration key 'Jwt:Audience' is required.")
        .AddErrorIfNullOrWhiteSpace(Jwt.EncryptionKey, "Configuration secret 'Jwt:EncryptionKey' is required (configure via User Secrets or environment variable 'Jwt__EncryptionKey').")
        .AddErrorIf(!string.IsNullOrWhiteSpace(Jwt.EncryptionKey) && Encoding.UTF8.GetByteCount(Jwt.EncryptionKey) != 32, "Configuration secret 'Jwt:EncryptionKey' must be exactly 32 bytes in UTF-8 for AES-256 KW.")
        .AddErrorIf(!isDevelopment && KnownPlaceholderSecrets.Contains(Jwt.EncryptionKey), "Configuration secret 'Jwt:EncryptionKey' must not use the quickstart placeholder value outside Development.")
        .AddErrorIfNullOrWhiteSpace(Jwt.SigningKey, "Configuration secret 'Jwt:SigningKey' is required (configure via User Secrets or environment variable 'Jwt__SigningKey').")
        .AddErrorIf(!isDevelopment && KnownPlaceholderSecrets.Contains(Jwt.SigningKey), "Configuration secret 'Jwt:SigningKey' must not use the quickstart placeholder value outside Development.")
        .AddErrorIfNull(Superuser, "Configuration section 'Superuser' is required.")
        .AddErrorIf(Superuser is not null && string.IsNullOrWhiteSpace(Superuser.Username), "Configuration key 'Superuser:Username' is required.")
        .AddErrorIf(Superuser is not null && string.IsNullOrWhiteSpace(Superuser.Password), "Configuration secret 'Superuser:Password' is required (configure via User Secrets or environment variable 'Superuser__Password').")
        .AddErrorIf(Superuser is not null && !string.IsNullOrWhiteSpace(Superuser.Password) && Superuser.Password.Length < 8, "Configuration secret 'Superuser:Password' must be at least 8 characters long.")
        .AddErrorIf(!isDevelopment && Superuser is not null && KnownPlaceholderSecrets.Contains(Superuser.Password), "Configuration secret 'Superuser:Password' must not use the quickstart placeholder value outside Development.")
        .AddErrorIf(FileStorage.Local is null || string.IsNullOrWhiteSpace(FileStorage.Local.BaseDirectory), "Configuration key 'FileStorage:Local:BaseDirectory' is required.")
        .AddErrorIfNegativeOrZero(ExperimentalData.MaxCsvSizeBytes, "Configuration key 'ExperimentalData:MaxCsvSizeBytes' must be greater than zero.");
}
