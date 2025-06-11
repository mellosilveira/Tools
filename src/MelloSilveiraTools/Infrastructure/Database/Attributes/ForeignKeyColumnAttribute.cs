using MelloSilveiraTools.Infrastructure.Database.Models;

namespace MelloSilveiraTools.Infrastructure.Database.Attributes;

/// <summary>
/// Specifies if the property correspond to foreign key column of table.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ForeignKeyColumnAttribute(Type referencedTableType, string referencedPropertyName, JoinType joinType = JoinType.Inner) : ColumnAttribute
{
    /// <inheritdoc cref="Models.JoinType"/>
    public string JoinType { get; } = joinType.ToString().ToUpperInvariant();

    /// <summary>
    /// Name of property which foreign key column is referenced.
    /// </summary>
    public string ReferencedPropertyName { get; } = referencedPropertyName;

    /// <summary>
    /// Type of table which foreign key is referenced.
    /// </summary>
    public Type ReferencedTableType { get; } = referencedTableType;
}
