using MelloSilveiraTools.Mathematics.Domain.NumericalMethods.DifferentialEquation;
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
            .AddSingleton<IDifferentialEquationMethod, NewmarkMethod>()
            .AddSingleton<IDifferentialEquationMethod, NewmarkBetaMethod>()
            // Register factories.
            .AddSingleton<DifferentialEquationMethodFactory>();
}
