using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter.Fung;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;

/// <summary>
/// Unit tests for <see cref="MechanicalModelStepFactory"/> covering creation of Fung, SimplifiedFung, and Schapery
/// curve fitter steps as well as unsupported model/segment combinations.
/// </summary>
public sealed class MechanicalModelStepFactoryTests
{
    private readonly Mock<IFungModelCalculator> _fungCalculatorMock = new();
    private readonly Mock<ISimplifiedFungModelCalculator> _simplifiedFungCalculatorMock = new();
    private readonly Mock<ISchaperyModelCalculator> _schaperyCalculatorMock = new();

    private MechanicalModelStepFactory CreateFactory()
    {
        ServiceCollection services = new();
        services.AddLogging();
        CurveFitterSettings fitterSettings = new();
        services.AddSingleton(fitterSettings);
        services.AddKeyedSingleton<ICurveFitter, AlglibCurveFitter>(CurveFitterType.Alglib);
        services.AddKeyedSingleton<ICurveFitter, MathNetCurveFitter>(CurveFitterType.MathNet);
        services.AddKeyedSingleton<IFunctionCurveFitter>(FunctionType.Logarithmic, (_, _) => new LogarithmicCurveFitter(new MathNetCurveFitter()));
        services.AddKeyedSingleton<IFunctionCurveFitter>(FunctionType.Exponential, (_, _) => new ExponentialCurveFitter(new MathNetCurveFitter()));
        services.AddKeyedSingleton<IFunctionCurveFitter>(FunctionType.Polynomial, (_, _) => new PolynomialCurveFitter(new MathNetCurveFitter()));
        services.AddSingleton<FunctionFactory>();
        services.AddSingleton(_fungCalculatorMock.Object);
        services.AddSingleton(_simplifiedFungCalculatorMock.Object);
        services.AddSingleton(_schaperyCalculatorMock.Object);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(serviceProvider);
        ServiceLocator serviceLocator = new(dynamicProvider, serviceProvider);

        return new MechanicalModelStepFactory(serviceLocator, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Verifies creation of the Fung relaxation-only curve fitter step with Alglib and MathNet fitters.
    /// </summary>
    [Theory]
    [InlineData(CurveFitterType.Alglib, 0.95, 0.99)]
    [InlineData(CurveFitterType.MathNet, 0.92, 0.90)]
    public void CreateCurveFitterStep_FungRelaxation_ReturnsExpectedStep(
        CurveFitterType fitterType,
        double targetRSquared,
        double confidenceLevel)
    {
        MechanicalModelStepFactory factory = CreateFactory();

        IMechanicalModelCurveFitterStep step = factory.CreateCurveFitterStep(
            nameof(MechanicalModel.Fung),
            [SegmentType.Relaxation],
            fitterType,
            targetRSquared,
            confidenceLevel);

        FungRelaxationOnlyCurveFitterStep fungStep = Assert.IsType<FungRelaxationOnlyCurveFitterStep>(step);
        Assert.Equal(targetRSquared, fungStep.TargetRSquared);
        Assert.Equal(confidenceLevel, fungStep.ConfidenceLevel);
    }

    /// <summary>
    /// Verifies creation of SimplifiedFung and Schapery relaxation-only curve fitter steps.
    /// </summary>
    [Fact]
    public void CreateCurveFitterStep_SimplifiedFungAndSchapery_ReturnsExpectedSteps()
    {
        MechanicalModelStepFactory factory = CreateFactory();

        IMechanicalModelCurveFitterStep simplifiedStep = factory.CreateCurveFitterStep(
            nameof(MechanicalModel.SimplifiedFung),
            [SegmentType.Relaxation],
            CurveFitterType.Alglib,
            0.94,
            0.95);

        SimplifiedFungRelaxationOnlyCurveFitterStep simplified = Assert.IsType<SimplifiedFungRelaxationOnlyCurveFitterStep>(simplifiedStep);
        Assert.Equal(0.94, simplified.TargetRSquared);

        IMechanicalModelCurveFitterStep schaperyStep = factory.CreateCurveFitterStep(
            nameof(MechanicalModel.Schapery),
            [SegmentType.Relaxation],
            CurveFitterType.MathNet,
            0.91,
            0.95);

        SchaperyRelaxationOnlyCurveFitterStep schapery = Assert.IsType<SchaperyRelaxationOnlyCurveFitterStep>(schaperyStep);
        Assert.Equal(0.91, schapery.TargetRSquared);
    }

    /// <summary>
    /// Verifies that unknown models or unsupported segment sets throw ArgumentException.
    /// </summary>
    [Fact]
    public void CreateCurveFitterStep_UnsupportedConfiguration_ThrowsArgumentException()
    {
        MechanicalModelStepFactory factory = CreateFactory();

        Assert.Throws<ArgumentException>(() => factory.CreateCurveFitterStep(
            "UnknownModel",
            [SegmentType.Relaxation],
            CurveFitterType.Alglib,
            0.95));

        Assert.Throws<ArgumentException>(() => factory.CreateCurveFitterStep(
            nameof(MechanicalModel.Fung),
            [SegmentType.Ramp],
            CurveFitterType.Alglib,
            0.95));
    }
}
