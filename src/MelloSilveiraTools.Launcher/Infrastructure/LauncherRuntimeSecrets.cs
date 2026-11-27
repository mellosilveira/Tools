using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MelloSilveiraTools.Launcher.Infrastructure;

/// <summary>
/// Resolves non-placeholder runtime secrets (<c>Jwt:EncryptionKey</c>, <c>Jwt:SigningKey</c>, and <c>Superuser:Password</c>)
/// required by <c>MelloSilveiraTools.WebApi.Host</c> outside Development.
/// Prefers explicit host environment variables and persists auto-generated local quickstart secrets
/// under the current user's profile directory so credentials remain stable across restarts.
/// </summary>
/// <param name="JwtEncryptionKey">32-byte UTF-8 AES-256-KW encryption key.</param>
/// <param name="JwtSigningKey">HMAC-SHA256 signing key.</param>
/// <param name="SuperuserPassword">Bootstrap password for the configured superuser account.</param>
/// <param name="SourceDescription">Human-readable origin of the resolved secrets (environment or file path).</param>
public sealed record LauncherRuntimeSecrets(
    string JwtEncryptionKey,
    string JwtSigningKey,
    string SuperuserPassword,
    string SourceDescription)
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Resolves secrets from host environment variables, falling back to a persistent per-user secrets file
    /// (<c>~/.mellosilveiratools/launcher-secrets.json</c>) created with cryptographically random values on first run.
    /// </summary>
    public static LauncherRuntimeSecrets ResolveOrCreate()
    {
        string? envEncryptionKey = ReadEnvironment("Jwt__EncryptionKey", "JWT__ENCRYPTIONKEY");
        string? envSigningKey = ReadEnvironment("Jwt__SigningKey", "JWT__SIGNINGKEY");
        string? envSuperuserPassword = ReadEnvironment("Superuser__Password", "SUPERUSER__PASSWORD");

        if (IsValidEncryptionKey(envEncryptionKey) && !string.IsNullOrWhiteSpace(envSigningKey) && IsValidPassword(envSuperuserPassword))
            return new LauncherRuntimeSecrets(envEncryptionKey!, envSigningKey!, envSuperuserPassword!, "environment variables");

        string secretsPath = GetDefaultSecretsFilePath();
        PersistedSecrets? persisted = TryReadPersistedSecrets(secretsPath);

        string encryptionKey = IsValidEncryptionKey(envEncryptionKey)
            ? envEncryptionKey!
            : IsValidEncryptionKey(persisted?.JwtEncryptionKey)
                ? persisted!.JwtEncryptionKey!
                : GenerateRandomAscii(32);

        string signingKey = !string.IsNullOrWhiteSpace(envSigningKey)
            ? envSigningKey!
            : !string.IsNullOrWhiteSpace(persisted?.JwtSigningKey)
                ? persisted!.JwtSigningKey!
                : GenerateRandomAscii(48);

        string superuserPassword = IsValidPassword(envSuperuserPassword)
            ? envSuperuserPassword!
            : IsValidPassword(persisted?.SuperuserPassword)
                ? persisted!.SuperuserPassword!
                : GenerateRandomAscii(20);

        bool needsPersist = persisted is null
            || persisted.JwtEncryptionKey != encryptionKey
            || persisted.JwtSigningKey != signingKey
            || persisted.SuperuserPassword != superuserPassword;

        if (needsPersist)
            TryWritePersistedSecrets(secretsPath, new PersistedSecrets(encryptionKey, signingKey, superuserPassword));

        return new LauncherRuntimeSecrets(encryptionKey, signingKey, superuserPassword, secretsPath);
    }

    /// <summary>
    /// Injects the resolved secrets into a child process environment dictionary.
    /// </summary>
    /// <param name="environment">Target <see cref="System.Diagnostics.ProcessStartInfo.Environment"/> dictionary.</param>
    /// <param name="uppercaseKeys">When <see langword="true"/>, emits uppercase Compose interpolation keys (<c>JWT__ENCRYPTIONKEY</c>, etc.); otherwise emits ASP.NET Core configuration keys.</param>
    public void ApplyToEnvironment(IDictionary<string, string?> environment, bool uppercaseKeys = false)
    {
        if (uppercaseKeys)
        {
            environment["JWT__ENCRYPTIONKEY"] = JwtEncryptionKey;
            environment["JWT__SIGNINGKEY"] = JwtSigningKey;
            environment["SUPERUSER__PASSWORD"] = SuperuserPassword;
            return;
        }

        environment["Jwt__EncryptionKey"] = JwtEncryptionKey;
        environment["Jwt__SigningKey"] = JwtSigningKey;
        environment["Superuser__Password"] = SuperuserPassword;
    }

    private static string GetDefaultSecretsFilePath()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".mellosilveiratools", "launcher-secrets.json");
    }

    private static string? ReadEnvironment(string primary, string secondary)
    {
        string? value = Environment.GetEnvironmentVariable(primary);
        return !string.IsNullOrWhiteSpace(value) ? value : Environment.GetEnvironmentVariable(secondary);
    }

    private static bool IsValidEncryptionKey(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Encoding.UTF8.GetByteCount(value) == 32;

    private static bool IsValidPassword(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 8;

    private static string GenerateRandomAscii(int length)
    {
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return new string(chars);
    }

    private static PersistedSecrets? TryReadPersistedSecrets(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<PersistedSecrets>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void TryWritePersistedSecrets(string path, PersistedSecrets secrets)
    {
        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(secrets, _jsonOptions);
            File.WriteAllText(path, json, Encoding.UTF8);

            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
        }
    }

    private sealed record PersistedSecrets(string? JwtEncryptionKey, string? JwtSigningKey, string? SuperuserPassword);
}
