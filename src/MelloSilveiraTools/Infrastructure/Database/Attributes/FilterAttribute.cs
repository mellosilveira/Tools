using MelloSilveiraTools.ExtensionMethods;
using System.Reflection;

namespace MelloSilveiraTools.Infrastructure.Database.Attributes;

/// <summary>
/// Specifies if the class is a filter for a table.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class FilterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterColumnAttribute" /> class.
    /// </summary>
    public FilterAttribute(Type entityToBeFiltered)
    {
        TableDefinition = entityToBeFiltered.GetCustomAttribute<TableAttribute>()!;
        JoinTablesDefinition = [];

        var properties = entityToBeFiltered.GetPropertiesInHierarchy();
        foreach (var property in properties)
        {
            var foreignKeyAttribute = property.GetCustomAttribute<ForeignKeyColumnAttribute>();
            if (foreignKeyAttribute != null)
            {
                TableAttribute? tableDefinitionAttribute = foreignKeyAttribute.ReferencedTableType.GetCustomAttribute<TableAttribute>();
                if (tableDefinitionAttribute != null)
                    JoinTablesDefinition.Add(tableDefinitionAttribute.Name, tableDefinitionAttribute);
            }
        }
    }

    /// <summary>
    /// Definition of main table.
    /// </summary>
    public TableAttribute TableDefinition { get; }

    /// <summary>
    /// Dictionary which key is the name of table in JOIN clause and value its definition.
    /// </summary>
    public Dictionary<string, TableAttribute> JoinTablesDefinition { get; }
}
