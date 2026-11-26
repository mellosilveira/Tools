using MelloSilveiraTools.Database.RelationalDatabase.Models;
using System.Diagnostics.CodeAnalysis;

namespace MelloSilveiraTools.Database.RelationalDatabase.Attributes;

/// <summary>
/// Specifies if the property corresponds to a foreign key column of a table.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ForeignKeyColumnAttribute(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type referencedTableType,
    JoinType joinType = JoinType.Inner) : ColumnAttribute
{
    /// <inheritdoc cref="Models.JoinType"/>
    public string JoinType { get; } = joinType.ToString().ToUpperInvariant();

    /// <summary>
    /// Type of table which foreign key is referenced.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type ReferencedTableType { get; } = referencedTableType;
}
