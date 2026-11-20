namespace MelloSilveiraTools.Database.Migrator;

/// <summary>
/// Defines the available contextual scopes for database schema migrations.
/// </summary>
/// <remarks>
/// <para>
/// <b>Notice:</b> The application exclusively supports PostgreSQL. All migrations target PostgreSQL dialect.
/// </para>
/// </remarks>
public enum MigrationContext
{
    /// <summary>
    /// Executes all migration scripts across every domain context (Common, Optimizations, Plugins).
    /// </summary>
    All = 0,

    /// <summary>
    /// Executes common baseline migrations only (e.g., stored_files).
    /// </summary>
    Common = 1,

    /// <summary>
    /// Executes experimental data, constitutive modeling, and parametric sweep migrations (includes Common as a prerequisite).
    /// </summary>
    Optimizations = 2,

    /// <summary>
    /// Executes dynamic plugin cache persistence migrations.
    /// </summary>
    Plugins = 3
}
