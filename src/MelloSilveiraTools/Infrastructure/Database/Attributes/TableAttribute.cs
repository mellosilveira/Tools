namespace MelloSilveiraTools.Infrastructure.Database.Attributes;

/// <summary>
/// Specifies the database table that a class is mapped to.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute(string name) : Attribute
{
    /// <summary>
    /// Name of table.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Alias of table.
    /// </summary>
    public string Alias { get; } = GetAliasName(name);

    /// <summary>
    /// Gets alias from table name.
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    private static string GetAliasName(string tableName)
    {
        char[] firstCharacters = tableName.Split('_').Select(s => s[0]).ToArray();
        return new string(firstCharacters);
    }
}