namespace MelloSilveiraTools.Infrastructure.Database.Attributes;

/// <summary>
/// Specifies if the property is a filter for a column on table.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FilterColumnAttribute(string filterClause, string? propertyName = null, string? tableName = null) : Attribute
{
    /// <summary>
    /// Clause to be used on filter. Example: equal, less, greater, LIKE, etc.
    /// </summary>
    public string FilterClause { get; } = filterClause;

    /// <summary>
    /// Name of table property to be filtered.
    /// </summary>
    public string? PropertyName { get; } = propertyName;

    /// <summary>
    /// Name of table. If not informed, correspond to main table.
    /// </summary>
    public string? TableName { get; } = tableName;
}
