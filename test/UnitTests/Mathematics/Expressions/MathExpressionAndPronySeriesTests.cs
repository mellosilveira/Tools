using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;

namespace UnitTests.Mathematics.Expressions;

/// <summary>
/// Unit tests for <see cref="MathExpression"/> and <see cref="PronySeries"/>.
/// </summary>
public sealed class MathExpressionAndPronySeriesTests
{
    [Fact]
    public void MathExpression_SortsWithoutMutatingCallerList_EvaluatesInDomainAndCachesCalculus()
    {
        ConstantFunction f2 = new(10.0, 5.0, 10.0);
        PolynomialFunction f1 = new([1.0, 2.0], 0.0, 6.0);
        List<Function> callerList = [f2, f1];

        MathExpression expr = new(MathExpressionType.Generic, callerList, 0.0, 10.0);

        // Caller list must remain unsorted while internal Functions list is sorted by InitialVariableValue.
        Assert.Same(f2, callerList[0]);
        Assert.Same(f1, expr[0]);
        Assert.Same(f2, expr[1]);
        Assert.Equal(MathExpressionType.Generic, expr.Type);
        Assert.Equal(0.0, expr.InitialVariableValue);
        Assert.Equal(10.0, expr.FinalVariableValue);

        // At x = 2: only f1 active -> 1 + 2*2 = 5.
        Assert.Equal(5.0, expr.Calculate(2.0), 10);
        // At x = 5.5: both f1 and f2 active -> (1 + 2*5.5) + 10 = 22.
        Assert.Equal(22.0, expr.Calculate(5.5), 10);
        // Outside domain of all functions: 0.
        Assert.Equal(0.0, expr.Calculate(15.0), 10);

        MathExpression deriv1 = expr.Derivative;
        MathExpression deriv2 = expr.Derivative;
        Assert.Same(deriv1, deriv2);
        Assert.Equal(2.0, deriv1.Calculate(2.0), 10);

        MathExpression integ1 = expr.Integral;
        MathExpression integ2 = expr.Integral;
        Assert.Same(integ1, integ2);
        // Integral of f1 (1 + 2x) at x = 2 is x + x^2 = 6.
        Assert.Equal(6.0, integ1.Calculate(2.0), 10);
    }

    [Fact]
    public void MathExpression_DefaultBoundsAndImplicitConversion_WorkCorrectly()
    {
        ConstantFunction fn = new(4.0, 1.0, 3.0);
        MathExpression implicitExpr = fn;

        Assert.Equal(MathExpressionType.Generic, implicitExpr.Type);
        Assert.Equal(1.0, implicitExpr.InitialVariableValue);
        Assert.Equal(3.0, implicitExpr.FinalVariableValue);
        Assert.Equal(4.0, implicitExpr.Calculate(2.0));

        MathExpression unbounded = new(MathExpressionType.Generic, [new ConstantFunction(3.0)]);
        Assert.Equal(double.NegativeInfinity, unbounded.InitialVariableValue);
        Assert.Equal(double.PositiveInfinity, unbounded.FinalVariableValue);
    }

    [Fact]
    public void PronySeries_EvaluatesSeriesDerivativeAndIntegralCorrectly()
    {
        double[] terms = [2.0, -0.5, 1.0, -2.0];
        PronySeries prony = new(5.0, terms, 0.0, 10.0);

        Assert.Equal(MathExpressionType.PronySeries, prony.Type);
        Assert.Equal(5.0, prony.IndependentParameter);
        Assert.Same(terms, prony.IteratorCoefficients);

        double t = 1.0;
        double expected = 5.0 + 2.0 * Math.Exp(-0.5 * t) + 1.0 * Math.Exp(-2.0 * t);
        Assert.Equal(expected, prony.Calculate(t), 10);

        double expectedDeriv = -1.0 * Math.Exp(-0.5 * t) - 2.0 * Math.Exp(-2.0 * t);
        Assert.Equal(expectedDeriv, prony.Derivative.Calculate(t), 10);

        double expectedInteg = 5.0 * t + (2.0 / -0.5) * Math.Exp(-0.5 * t) + (1.0 / -2.0) * Math.Exp(-2.0 * t);
        Assert.Equal(expectedInteg, prony.Integral.Calculate(t), 10);
    }

    [Fact]
    public void MathExpression_JsonSerialization_IgnoresDerivativeAndIntegralToAvoidInfiniteRecursion()
    {
        MathExpression expr = new(MathExpressionType.Generic, [new PolynomialFunction([1.0, 2.0], 0.0, 5.0)], 0.0, 5.0);

        string json = System.Text.Json.JsonSerializer.Serialize(expr);

        Assert.DoesNotContain($"\"{nameof(MathExpression.Derivative)}\"", json);
        Assert.DoesNotContain($"\"{nameof(MathExpression.Integral)}\"", json);
    }
}
