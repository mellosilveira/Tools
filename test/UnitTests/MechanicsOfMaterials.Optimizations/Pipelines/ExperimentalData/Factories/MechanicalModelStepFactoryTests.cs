namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;

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
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter.Fung;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

public class MechanicalModelStepFactoryTests
{
    private readonly Mock<IFungModelCalculator> _fungCalculatorMock = new();
    private readonly Mock<ISimplifiedFungModelCalculator> _simplifiedFungCalculatorMock = new();
    private readonly Mock<ISchaperyModelCalculator> _schaperyCalculatorMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();

    private MechanicalModelStepFactory CreateFactory()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<ICurveFitter, AlglibCurveFitter>(CurveFitterType.Alglib);
        services.AddKeyedSingleton<ICurveFitter, MathNetCurveFitter>(CurveFitterType.MathNet);
        services.AddKeyedSingleton<IFunctionCurveFitter>(FunctionType.Logarithmic, (sp, key) => new LogarithmicCurveFitter(new AlglibCurveFitter(Mock.Of<ILogger<AlglibCurveFitter>>())));
        services.AddKeyedSingleton<IFunctionCurveFitter>(FunctionType.Exponential, (sp, key) => new ExponentialCurveFitter(new AlglibCurveFitter(Mock.Of<ILogger<AlglibCurveFitter>>())));
        services.AddKeyedSingleton<IFunctionCurveFitter>(FunctionType.Polynomial, (sp, key) => new PolynomialCurveFitter(new AlglibCurveFitter(Mock.Of<ILogger<AlglibCurveFitter>>())));
        services.AddSingleton<FunctionFactory>();
        services.AddSingleton(_fungCalculatorMock.Object);
        services.AddSingleton(_simplifiedFungCalculatorMock.Object);
        services.AddSingleton(_schaperyCalculatorMock.Object);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(serviceProvider);
        ServiceLocator serviceLocator = new(dynamicProvider, serviceProvider);

        return new MechanicalModelStepFactory(
            serviceLocator,
            _loggerFactoryMock.Object);
    }

    [Fact]
    public void Create_FungWithAlglib_InstantiatesStepWithMandatoryTargetRSquared()
    {
        MechanicalModelStepFactory factory = CreateFactory();
        const double targetRSquared = 0.95;

        IMechanicalModelCurveFitterStep step = factory.Create(
            nameof(MechanicalModel.Fung),
            [SegmentType.Relaxation],
            CurveFitterType.Alglib,
            targetRSquared);

        FungRelaxationOnlyCurveFitterStep fungStep = Assert.IsType<FungRelaxationOnlyCurveFitterStep>(step);
        Assert.Equal(targetRSquared, fungStep.TargetRSquared);
    }

    [Fact]
    public void Create_FungWithMathNet_InstantiatesStepWithMandatoryTargetRSquared()
    {
        MechanicalModelStepFactory factory = CreateFactory();
        const double targetRSquared = 0.92;

        IMechanicalModelCurveFitterStep step = factory.Create(
            nameof(MechanicalModel.Fung),
            [SegmentType.Relaxation],
            CurveFitterType.MathNet,
            targetRSquared);

        FungRelaxationOnlyCurveFitterStep fungStep = Assert.IsType<FungRelaxationOnlyCurveFitterStep>(step);
        Assert.Equal(targetRSquared, fungStep.TargetRSquared);
    }

    [Fact]
    public void Create_SimplifiedFung_InstantiatesStepWithMandatoryTargetRSquared()
    {
        MechanicalModelStepFactory factory = CreateFactory();
        const double targetRSquared = 0.94;

        IMechanicalModelCurveFitterStep step = factory.Create(
            nameof(MechanicalModel.SimplifiedFung),
            [SegmentType.Relaxation],
            CurveFitterType.Alglib,
            targetRSquared);

        SimplifiedFungRelaxationOnlyCurveFitterStep simplifiedStep = Assert.IsType<SimplifiedFungRelaxationOnlyCurveFitterStep>(step);
        Assert.Equal(targetRSquared, simplifiedStep.TargetRSquared);
    }

    [Fact]
    public void Create_Schapery_InstantiatesStepWithMandatoryTargetRSquared()
    {
        MechanicalModelStepFactory factory = CreateFactory();
        const double targetRSquared = 0.91;

        IMechanicalModelCurveFitterStep step = factory.Create(
            nameof(MechanicalModel.Schapery),
            [SegmentType.Relaxation],
            CurveFitterType.MathNet,
            targetRSquared);

        SchaperyRelaxationOnlyCurveFitterStep schaperyStep = Assert.IsType<SchaperyRelaxationOnlyCurveFitterStep>(step);
        Assert.Equal(targetRSquared, schaperyStep.TargetRSquared);
    }

    [Fact]
    public void Create_UnknownModel_ThrowsArgumentException()
    {
        MechanicalModelStepFactory factory = CreateFactory();

        Assert.Throws<ArgumentException>(() => factory.Create(
            "UnknownModel",
            [SegmentType.Relaxation],
            CurveFitterType.Alglib,
            0.90));
    }

    [Fact]
    public void Create_UnsupportedSegments_ThrowsArgumentException()
    {
        MechanicalModelStepFactory factory = CreateFactory();

        Assert.Throws<ArgumentException>(() => factory.Create(
            nameof(MechanicalModel.Fung),
            [SegmentType.Descent],
            CurveFitterType.Alglib,
            0.90));
    }
}
