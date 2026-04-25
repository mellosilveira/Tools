namespace MelloSilveiraTools.Database.Infrastructure.Database.Attributes;

/// <summary>
/// Specifies the database table that a class is mapped to.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableAttribute"/> class with an explicit table alias.
    /// </summary>
    /// <param name="name">Name of the database table.</param>
    /// <param name="alias">Alias used to qualify the table in SQL statements.</param>
    public TableAttribute(string name, string alias)
    {
        Name = name;
        Alias = alias;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableAttribute"/> class, deriving the alias from the table name.
    /// </summary>
    /// <param name="name">Name of the database table.</param>
    public TableAttribute(string name)
    {
        Name = name;
        Alias = GetAliasName(name);
    }

    /// <summary>
    /// Name of table.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Alias of table.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Gets alias from table name.
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    private static string GetAliasName(string tableName)
    {
        if (!tableName.Contains('_'))
            return tableName;

        char[] firstCharacters = [.. tableName.Split('_').Select(s => s[0])];
        return new string(firstCharacters);
    }
}