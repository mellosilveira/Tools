namespace MelloSilveiraTools.Core.Models;

/// <summary>
/// Standard separators used when serializing/deserializing CSV files.
/// </summary>
public static class CsvSeparators
{
    /// <summary>
    /// Main column separator. Standard CSV (comma-separated).
    /// </summary>
    public const char Main = ',';

    /// <summary>
    /// Secondary separator used inside cells when a single column carries multiple values
    /// (e.g. an array serialized as <c>"1;2;3"</c> within one CSV column).
    /// </summary>
    public const char Secondary = ';';
}
