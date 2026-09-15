namespace MelloSilveiraTools.Database.RelationalDatabase.Attributes;

/// <summary>
/// Specifies if a column is a primary key of table.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PrimaryKeyColumnAttribute : ColumnAttribute;
