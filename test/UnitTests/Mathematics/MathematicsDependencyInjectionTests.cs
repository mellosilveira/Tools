using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.DifferentialEquation;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.Mathematics.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Mathematics;

/// <summary>
/// Unit tests for <see cref="MathematicsDependencyInjection"/> verifying all registered mathematical services.
/// </summary>
public sealed class MathematicsDependencyInjectionTests
{
    [Fact]
    public void AddMathematicsServices_RegistersExpectedServicesAndKeyedImplementations()
    {
        ServiceCollection services = new();
        services.AddLogging();

        IServiceCollection returned = services.AddMathematicsServices();

        Assert.Same(services, returned);

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<Differentiation>(provider.GetRequiredService<IDifferentiation>());
        Assert.IsType<SimpsonRuleIntegration>(provider.GetRequiredService<IIntegration>());
        Assert.NotNull(provider.GetRequiredService<FunctionFactory>());
        Assert.IsType<StatisticsCalculator>(provider.GetRequiredService<IStatisticsCalculator>());

        Assert.IsType<NewmarkMethod>(provider.GetRequiredKeyedService<IDifferentialEquationMethod>(DifferentialEquationMethodType.Newmark));
        Assert.IsType<NewmarkBetaMethod>(provider.GetRequiredKeyedService<IDifferentialEquationMethod>(DifferentialEquationMethodType.NewmarkBeta));

        Assert.IsType<BisectionMethod>(provider.GetRequiredKeyedService<IRootFinding>(RootFindingAlgorithm.BisectionMethod));
        Assert.IsType<BrentMethod>(provider.GetRequiredKeyedService<IRootFinding>(RootFindingAlgorithm.BrentMethod));
        Assert.IsType<StepByStepMethod>(provider.GetRequiredKeyedService<IRootFinding>(RootFindingAlgorithm.StepByStepMethod));
    }
}
