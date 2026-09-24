using System.Linq.Expressions;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses;

public static class ExpressionPathResolver
{
    /// <summary>
    /// Converts a lambda expression into a string path using the built-in C# compiler expression tree.
    /// </summary>
    public static string GetPath<T, TProperty>(Expression<Func<T, TProperty>> expression) => BuildPath(expression.Body);

    private static string BuildPath(Expression expression) => expression switch
    {
        // Handles standard properties (e.g., x.YoungModulus)
        MemberExpression member => member.Expression != null && member.Expression.NodeType != ExpressionType.Parameter
            ? $"{BuildPath(member.Expression)}.{member.Member.Name}"
            : member.Member.Name,

        // Handles array indexing (e.g., IteratorCoefficients[0])
        BinaryExpression binary when binary.NodeType == ExpressionType.ArrayIndex => $"{BuildPath(binary.Left)}[{GetConstantValue(binary.Right)}]",

        // Handles unboxing/casting if the compiler injects a Convert node
        UnaryExpression unary => BuildPath(unary.Operand),

        _ => throw new ArgumentException($"Expression type {expression.NodeType} is not supported.")
    };

    private static string GetConstantValue(Expression expression) => expression is ConstantExpression constant
        ? constant.Value?.ToString() ?? "0"
        : throw new ArgumentException("Array index must be a constant value.");
}