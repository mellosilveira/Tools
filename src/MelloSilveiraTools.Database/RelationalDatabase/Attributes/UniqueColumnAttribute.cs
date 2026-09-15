namespace MelloSilveiraTools.Database.RelationalDatabase.Attributes;

/// <summary>
/// Specifies that a column has a unique constraint, used for ON CONFLICT upserts and typed lookups via GetByUniqueColumnAsync.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class UniqueColumnAttribute : ColumnAttribute;
