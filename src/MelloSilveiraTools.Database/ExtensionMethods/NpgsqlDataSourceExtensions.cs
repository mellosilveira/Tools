using Microsoft.Extensions.Logging;
using Npgsql;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="NpgsqlDataSource"/>.
/// </summary>
public static class NpgsqlDataSourceExtensions
{
    extension(NpgsqlDataSource dataSource)
    {
        /// <summary>
        /// Creates a new <see cref="NpgsqlBatch"/> from <paramref name="dataSource"/> and applies
        /// <paramref name="commandTimeoutSeconds"/> when specified.
        /// </summary>
        /// <param name="commandTimeoutSeconds">Optional per-command timeout in seconds.</param>
        /// <returns>A configured <see cref="NpgsqlBatch"/> bound to <paramref name="dataSource"/>.</returns>
        public NpgsqlBatch CreateBatch(int? commandTimeoutSeconds)
        {
            NpgsqlBatch batch = dataSource.CreateBatch();
            if (commandTimeoutSeconds.HasValue)
                batch.Timeout = commandTimeoutSeconds.Value;

            return batch;
        }

        /// <summary>
        /// Verifies that the PostgreSQL role used by <paramref name="dataSource"/> does not carry
        /// <c>SUPERUSER</c> or <c>BYPASSRLS</c> privileges, which would silently bypass Row-Level Security.
        /// </summary>
        /// <param name="logger">The logger to use for logging messages.</param>
        /// <param name="allowSuperuser">
        /// When <see langword="true"/>, skips the role verification (explicit development opt-out).
        /// </param>
        /// <param name="ignoreConnectionErrors">
        /// When <see langword="true"/>, ignores transient <see cref="NpgsqlException"/> connectivity errors
        /// (useful in development while PostgreSQL is still starting up).
        /// </param>
        /// <param name="cancellationToken">Token used to cancel the verification query.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="allowSuperuser"/> is <see langword="false"/> and the active PostgreSQL role has
        /// <c>SUPERUSER</c> or <c>BYPASSRLS</c> privileges.
        /// </exception>
        public async Task EnsureNonSuperuserRoleAsync(ILogger logger, bool allowSuperuser = false, bool ignoreConnectionErrors = false, CancellationToken cancellationToken = default)
        {
            if (allowSuperuser)
                return;

            try
            {
                await using NpgsqlCommand guardCommand = dataSource.CreateCommand("SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname = current_user;");
                object? bypassResult = await guardCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (bypassResult is true)
                    throw new InvalidOperationException("Refusing to start: the configured PostgreSQL role has SUPERUSER or BYPASSRLS privileges, which silently disables Row-Level Security. Connect using a non-privileged role (e.g. 'tools_app') or set 'Database:AllowSuperuserDatabaseConnection=true' explicitly in Development.");
            }
            catch (NpgsqlException) when (ignoreConnectionErrors)
            {
                // Allow startup in Development when PostgreSQL is still initializing; health checks and runtime queries report connectivity status.
                logger.LogWarning("Ignoring PostgreSQL connection error during role verification.");
            }
        }

        /// <summary>
        /// Executes a lightweight <c>SELECT 1</c> connectivity probe against PostgreSQL.
        /// </summary>
        /// <param name="commandTimeoutSeconds">Optional command timeout in seconds applied to the probe command.</param>
        /// <param name="cancellationToken">Token used to cancel the connectivity probe.</param>
        public async Task PingAsync(int commandTimeoutSeconds, CancellationToken cancellationToken = default)
        {
            await using NpgsqlCommand command = dataSource.CreateCommand("SELECT 1;");
            command.CommandTimeout = commandTimeoutSeconds;

            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
