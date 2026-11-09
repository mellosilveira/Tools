namespace MelloSilveiraTools.Database.Migrator;

/// <summary>
/// Configuration options for executing contextual database schema migrations.
/// </summary>
/// <remarks>
/// <para>
/// <b>Notice:</b> The application exclusively supports PostgreSQL. The connection string must target a PostgreSQL database instance.
/// </para>
/// </remarks>
/// <param name="ConnectionString">The PostgreSQL connection string.</param>
/// <param name="Context">The target domain migration context.</param>
/// <param name="CommandTimeoutInSeconds">Timeout in seconds for individual DDL script execution.</param>
public record MigratorOptions(
    string ConnectionString,
    MigrationContext Context = MigrationContext.All,
    int CommandTimeoutInSeconds = 60);
