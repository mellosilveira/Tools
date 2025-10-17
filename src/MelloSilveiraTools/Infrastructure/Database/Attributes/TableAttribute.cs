namespace MelloSilveiraTools.Infrastructure.Database.Attributes;

/// <summary>
/// Specifies the database table that a class is mapped to.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute : Attribute
{
    public TableAttribute(string name, string alias)
    {
        Name = name;
        Alias = alias;
    }

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