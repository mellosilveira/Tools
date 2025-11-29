using MelloSilveiraTools.Domain.Models;
using System.Text;

namespace MelloSilveiraTools.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="string"/>>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Adds spaces before upper case.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string AddSpaceBeforeUpperCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        SpanStringBuilder result = new();
        for (int i = 0; i < input.Length; i++)
        {
            if (i > 0 && char.IsUpper(input[i]) && !char.IsWhiteSpace(input[i - 1]))
                result.Append(' ');

            result.Append(input[i]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a string from PascalCase or camelCase to snake_case.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
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
    /// <param name="input"></param>
    /// <returns></returns>
    public static string FromSnakeCaseToCamelCase(this string input)
    {
        string[] parts = input.Split('_');
        StringBuilder result = new(parts[0]);

        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
                result.Append(char.ToUpper(parts[i][0]) + parts[i][1..].ToLowerInvariant());
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a string from snake_case to PascalCase.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string FromSnakeCaseToPascalCase(this string input)
    {
        string[] parts = input.Split('_');
        StringBuilder result = new();

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
                result.Append(char.ToUpper(parts[i][0]) + parts[i][1..].ToLowerInvariant());
        }

        return result.ToString();
    }

    /// <summary>
    /// Removes a string frmo another.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="valuesToRemove"></param>
    /// <returns></returns>
    public static string Remove(this string input, params string[] valuesToRemove)
    {
        string result = input;
        foreach (string valueToRemove in valuesToRemove)
        {
            result = input.Replace(valueToRemove, null);
        }

        return result;
    }
}