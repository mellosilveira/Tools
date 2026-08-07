using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.DifferentialEquation;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integral;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.Mathematics.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Mathematics;

/// <summary>
/// Provides extension methods to register services from the MelloSilveiraTools.Mathematics package.
/// </summary>
public static class MathematicsDependencyInjection
{
    /// <summary>
    /// Register numerical methods (Newmark, Newmark-β) and the differential-equation method factory.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddMathematicsServices(this IServiceCollection services)
        => services
            // Register numerical methods.
            .AddSingleton<IDifferentiation, Differentiation>()
            .AddKeyedSingleton<IDifferentialEquationMethod, NewmarkMethod>(DifferentialEquationMethodType.Newmark)
            .AddKeyedSingleton<IDifferentialEquationMethod, NewmarkBetaMethod>(DifferentialEquationMethodType.NewmarkBeta)
            .AddSingleton<IIntegration, SimpsonRuleIntegration>()
            .AddKeyedSingleton<IRootFinding, BisectionMethod>(RootFindingAlgorithm.BisectionMethod)
            .AddKeyedSingleton<IRootFinding, BrentMethod>(RootFindingAlgorithm.BrentMethod)
            .AddKeyedSingleton<IRootFinding, StepByStepMethod>(RootFindingAlgorithm.StepByStepMethod)
            // Register factories.
            .AddSingleton<FunctionFactory>()
            // Register calculators.
            .AddSingleton<IStatisticsCalculator, StatisticsCalculator>();
}
