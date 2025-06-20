using MelloSilveiraTools.Infrastructure.Database.Models;

namespace MelloSilveiraTools.Infrastructure.Database.Attributes;

/// <summary>
/// Specifies if the property correspond to foreign key column of table.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ForeignKeyColumnAttribute(Type referencedTableType, JoinType joinType = JoinType.Inner) : ColumnAttribute
{
    /// <inheritdoc cref="Models.JoinType"/>
    public string JoinType { get; } = joinType.ToString().ToUpperInvariant();

    /// <summary>
    /// Type of table which foreign key is referenced.
    /// </summary>
    public Type ReferencedTableType { get; } = referencedTableType;
}
