using MelloSilveiraTools.Core.ExtensionMethods;
using Npgsql;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains Native AOT-compatible result-set navigation and materialization extensions for <see cref="NpgsqlDataReader"/>.
/// </summary>
public static class NpgsqlDataReaderExtensions
{
    extension(NpgsqlDataReader reader)
    {
        /// <summary>
        /// Advances <paramref name="reader"/> forward by <paramref name="resultSetIndex"/> result sets using
        /// <see cref="NpgsqlDataReader.NextResultAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="resultSetIndex">Zero-based index of the target result set.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous advance.</param>
        /// <returns><see langword="true"/> if the target result set is available; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="resultSetIndex"/> is negative.</exception>
        public async Task<bool> AdvanceToResultSetAsync(int resultSetIndex, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < resultSetIndex; i++)
            {
                if (!await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Advances to <paramref name="resultSetIndex"/> and reads the first column of the first row converted to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Expected scalar return type.</typeparam>
        /// <param name="resultSetIndex">Zero-based index of the target result set.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous read.</param>
        /// <returns>The converted scalar value, or <c>default</c> when the result set is missing, empty, or SQL <c>NULL</c>.</returns>
        public async Task<T?> ReadScalarAsync<T>(int resultSetIndex = 0, CancellationToken cancellationToken = default)
        {
            return !await reader.AdvanceToResultSetAsync(resultSetIndex, cancellationToken).ConfigureAwait(false) || !await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                ? default
                : ConvertScalar<T>(reader.GetValue(0));
        }

        /// <summary>
        /// Advances to <paramref name="resultSetIndex"/> and materializes the first row as <typeparamref name="T"/>,
        /// or returns <see langword="null"/> when no row is available or the first column is SQL <c>NULL</c>.
        /// </summary>
        /// <typeparam name="T">Target entity type whose public properties are preserved for trimming and Native AOT.</typeparam>
        /// <param name="resultSetIndex">Zero-based index of the target result set.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous read.</param>
        /// <returns>The first mapped entity, or <see langword="null"/>.</returns>
        public async Task<T?> ReadFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(int resultSetIndex = 0, CancellationToken cancellationToken = default) where T : class, new()
        {
            if (!await reader.AdvanceToResultSetAsync(resultSetIndex, cancellationToken).ConfigureAwait(false) || !await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return null;

            return await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
                ? null
                : reader.ConvertTo<T>();
        }

        /// <summary>
        /// Advances to <paramref name="resultSetIndex"/> and streams every row whose first column is not SQL <c>NULL</c>
        /// materialized as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target entity type whose public properties are preserved for trimming and Native AOT.</typeparam>
        /// <param name="resultSetIndex">Zero-based index of the target result set.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous stream.</param>
        /// <returns>An asynchronous stream of mapped entities.</returns>
        public async IAsyncEnumerable<T> StreamAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            int resultSetIndex = 0,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where T : class, new()
        {
            if (!await reader.AdvanceToResultSetAsync(resultSetIndex, cancellationToken).ConfigureAwait(false))
                yield break;

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                    yield return reader.ConvertTo<T>();
            }
        }
    }

    internal static T? ConvertScalar<T>(object? rawValue)
    {
        if (rawValue is null or DBNull)
            return default;

        if (rawValue is T typedValue)
            return typedValue;

        Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
    }
}
