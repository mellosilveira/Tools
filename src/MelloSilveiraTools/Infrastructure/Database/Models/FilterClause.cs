namespace MelloSilveiraTools.Infrastructure.Database.Models;

/// <summary>
/// SQL comparison operators used by filter columns when building WHERE clauses.
/// </summary>
public class FilterClause
{
    /// <summary>Equality comparison (=).</summary>
    public const string Equal = "=";

    /// <summary>Inequality comparison (!=).</summary>
    public const string NotEqual = "!=";

    /// <summary>Case-sensitive pattern matching (LIKE).</summary>
    public const string Like = "LIKE";

    /// <summary>Case-insensitive pattern matching (ILIKE).</summary>
    public const string ILike = "ILIKE";

    /// <summary>Greater than comparison (&gt;).</summary>
    public const string GreaterThan = ">";

    /// <summary>Less than comparison (&lt;).</summary>
    public const string LessThan = "<";

    /// <summary>Greater than or equal comparison (&gt;=).</summary>
    public const string GreaterThanOrEqual = ">=";

    /// <summary>Less than or equal comparison (&lt;=).</summary>
    public const string LessThanOrEqual = "<=";
}
