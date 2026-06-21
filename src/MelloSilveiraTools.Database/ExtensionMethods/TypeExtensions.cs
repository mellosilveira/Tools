using NpgsqlTypes;
using System.Collections;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    extension(Type type)
    {
        /// <summary>
        /// Returns the <see cref="NpgsqlDbType"/> from property type.
        /// </summary>
        /// <remarks>
        /// <paramref name="type"/>: The CLR property type to be mapped to its corresponding <see cref="NpgsqlDbType"/>.
        /// </remarks>
        /// <returns>The <see cref="NpgsqlDbType"/> matching <paramref name="type"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="type"/> is not handled by this mapping (i.e. it is not one of the supported
        /// primitive, nullable primitive, array or generic enumerable types).
        /// </exception>
        public NpgsqlDbType GetDbTypeFromPropertyType()
        {
            if (type == typeof(string)) return NpgsqlDbType.Text;
            if (type == typeof(bool) || type == typeof(bool?)) return NpgsqlDbType.Boolean;
            if (type == typeof(short) || type == typeof(short?)) return NpgsqlDbType.Smallint;
            if (type == typeof(int) || type == typeof(int?)) return NpgsqlDbType.Integer;
            if (type == typeof(long) || type == typeof(long?)) return NpgsqlDbType.Bigint;
            if (type == typeof(float) || type == typeof(float?)) return NpgsqlDbType.Real;
            if (type == typeof(double) || type == typeof(double?)) return NpgsqlDbType.Double;
            if (type == typeof(decimal) || type == typeof(decimal?)) return NpgsqlDbType.Numeric;
            if (type == typeof(byte[])) return NpgsqlDbType.Bytea;
            if (type == typeof(string[])) return NpgsqlDbType.Text | NpgsqlDbType.Array;
            if (type == typeof(DateTime) || type == typeof(DateTime?)) return NpgsqlDbType.Timestamp;
            if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?)) return NpgsqlDbType.TimestampTz;
            if (type == typeof(IList) || type == typeof(IEnumerable) || type == typeof(IEnumerator)) return NpgsqlDbType.Array;
            throw new ArgumentOutOfRangeException(nameof(type), $"Invalid type: '{type.FullName}'.");
        }
    }
}
