using MelloSilveiraTools.Core.Models;
using System.Text;

namespace MelloSilveiraTools.Core.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Inserts a space character before every uppercase letter (except at the start of the string,
    /// or when the previous character is already whitespace). Useful for converting an identifier
    /// such as <c>"MyClassName"</c> into the human-readable form <c>"My Class Name"</c>.
    /// </summary>
    /// <param name="input">The source string. If <see langword="null"/> or empty, it is returned unchanged.</param>
    /// <returns>The input string with a space inserted before each uppercase letter that does not already follow whitespace.</returns>
    public static string AddSpaceBeforeUpperCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        using SpanStringBuilder value = new();
        for (int i = 0; i < input.Length; i++)
        {
            if (i > 0 && char.IsUpper(input[i]) && !char.IsWhiteSpace(input[i - 1]))
                value.Append(' ');

            value.Append(input[i]);
        }

        return value.ToString();
    }

    /// <summary>
    /// Converts a string from PascalCase or camelCase to snake_case.
    /// </summary>
    /// <param name="input">The source identifier in PascalCase or camelCase. If <see langword="null"/> or empty, it is returned unchanged.</param>
    /// <returns>The snake_case representation of <paramref name="input"/> (all lowercase, with underscores inserted before each original uppercase boundary).</returns>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var snakeCase = new StringBuilder();
        snakeCase.Append(char.ToLowerInvariant(input[0]));
        for (int i = 1; i < input.Length; ++i)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                snakeCase.Append('_');
                snakeCase.Append(char.ToLowerInvariant(c));
            }
            else
            {
                snakeCase.Append(c);
            }
        }

        return snakeCase.ToString();
    }

    /// <summary>
    /// Converts a string from snake_case to camelCase.
    /// </summary>
    /// <param name="input">The source string in snake_case. The first segment is kept as-is (lowercase by convention) and every subsequent underscore-separated segment has its first letter uppercased.</param>
    /// <returns>The camelCase representation of <paramref name="input"/>.</returns>
    public static string FromSnakeCaseToCamelCase(this string input)
    {
        string[] parts = input.Split('_');
        StringBuilder value = new(parts[0]);

        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
                value.Append(char.ToUpper(parts[i][0]) + parts[i][1..].ToLowerInvariant());
        }

        return value.ToString();
    }

    /// <summary>
    /// Converts a string from snake_case to PascalCase.
    /// </summary>
    /// <param name="input">The source string in snake_case. Each underscore-separated segment has its first letter uppercased and the remainder lowercased.</param>
    /// <returns>The PascalCase representation of <paramref name="input"/>.</returns>
    public static string FromSnakeCaseToPascalCase(this string input)
    {
        string[] parts = input.Split('_');
        StringBuilder value = new();

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
                value.Append(char.ToUpper(parts[i][0]) + parts[i][1..].ToLowerInvariant());
        }

        return value.ToString();
    }

    /// <summary>
    /// Removes every occurrence of each supplied substring from <paramref name="input"/>. The substrings
    /// are removed sequentially in the order they are provided, so a later argument operates on the
    /// result of the previous removal (this matters when one substring may be produced by another's removal).
    /// </summary>
    /// <param name="input">The source string from which substrings will be removed.</param>
    /// <param name="valuesToRemove">The substrings to remove. All occurrences of each value are removed; the order of arguments defines the order of removals.</param>
    /// <returns>A new string with every occurrence of each value in <paramref name="valuesToRemove"/> stripped out.</returns>
    public static string Remove(this string input, params string[] valuesToRemove)
    {
        string value = input;
        foreach (string valueToRemove in valuesToRemove)
        {
            value = value.Replace(valueToRemove, null);
        }

        return value;
    }
}