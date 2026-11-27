namespace MelloSilveiraTools.Launcher.Infrastructure;

/// <summary>
/// Defines the contextual scopes passed to <c>MelloSilveiraTools.Database.Migrator</c>.
/// </summary>
public enum LauncherMigrationContext
{
    /// <summary>
    /// Executes all migration scripts across every domain context (Common, Optimizations, Plugins).
    /// </summary>
    All = 0,

    /// <summary>
    /// Executes common baseline migrations only (<c>users</c>, <c>storage_entry_metadata</c>).
    /// </summary>
    Common = 1,

    /// <summary>
    /// Executes experimental data, constitutive modeling, and parametric sweep migrations (includes Common as a prerequisite).
    /// </summary>
    Optimizations = 2,

    /// <summary>
    /// Executes dynamic plugin cache and submission migrations.
    /// </summary>
    Plugins = 3
}
