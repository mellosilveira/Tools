using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.SensitivityAnalyses;

public sealed class AsymptoteCalculationStepTests
{
    [Fact]
    public void Execute_Should_DetectAsymptoteTimeAndStress_ForExponentialDecay()
    {
        // Arrange
        Mock<IMechanicalModelCalculatorFacade> calculatorMock = new();
        calculatorMock
            .Setup(c => c.CalculateStress(It.IsAny<GenericMechanicalModelInput>(), It.IsAny<double>(), It.IsAny<double?>()))
            .Returns<GenericMechanicalModelInput, double, double?>((_, time, _) =>
            {
                // Decays from 10.0 at t=0 to 2.0 as t -> infty with time constant tau = 10.0
                return 2.0 + 8.0 * Math.Exp(-time / 10.0);
            });

        Differentiation differentiation = new();
        AsymptoteCalculationStep step = new(calculatorMock.Object, differentiation, 1e-3);

        GenericMechanicalModelInput input = new()
        {
            MechanicalModelName = "TestModel",
            TimeStep = 0.1,
            ConstitutiveParameters = new FungConstitutiveParameters(100.0, 1.0, new ReducedRelaxationFunction(0.1, 0.01, 10.0))
        };

        // Act
        (double asymptoteTime, double asymptoteStress) = step.Execute(input);

        // Assert
        // Steady state plateau reached within derivative tolerance
        Assert.True(Math.Abs(asymptoteStress - 2.0) <= 0.02, $"Expected asymptoteStress ~ 2.0, got {asymptoteStress}");

        // Asymptote is reached when |dσ/dt| <= 0.001
        Assert.True(asymptoteTime >= 60.0 && asymptoteTime <= 80.0, $"Expected asymptoteTime ~ 69.07, got {asymptoteTime}");
    }

    [Fact]
    public void Execute_Should_ReturnInitialTime_WhenDeltaIsNegligible()
    {
        // Arrange
        Mock<IMechanicalModelCalculatorFacade> calculatorMock = new();
        calculatorMock
            .Setup(c => c.CalculateStress(It.IsAny<GenericMechanicalModelInput>(), It.IsAny<double>(), It.IsAny<double?>()))
            .Returns(5.0); // Constant elastic response, no relaxation

        Differentiation differentiation = new();
        AsymptoteCalculationStep step = new(calculatorMock.Object, differentiation, 1e-3);

        GenericMechanicalModelInput input = new()
        {
            MechanicalModelName = "ElasticModel",
            RampTime = 1.5,
            TimeStep = 0.1,
            ConstitutiveParameters = new FungConstitutiveParameters(100.0, 1.0, new ReducedRelaxationFunction(0.1, 0.01, 10.0))
        };

        // Act
        (double asymptoteTime, double asymptoteStress) = step.Execute(input);

        // Assert
        Assert.Equal(1.5, asymptoteTime);
        Assert.Equal(5.0, asymptoteStress);
    }

    [Fact]
    public void Execute_Should_SupportCustomDifferentiationEngine_AndDetectAsymptote()
    {
        // Arrange
        Mock<IMechanicalModelCalculatorFacade> calculatorMock = new();
        calculatorMock
            .Setup(c => c.CalculateStress(It.IsAny<GenericMechanicalModelInput>(), It.IsAny<double>(), It.IsAny<double?>()))
            .Returns<GenericMechanicalModelInput, double, double?>((_, time, _) =>
            {
                // Decays from 10.0 at t=0 to 1.0 as t -> infty with time constant tau = 5.0
                return 1.0 + 9.0 * Math.Exp(-time / 5.0);
            });

        Differentiation differentiation = new();
        AsymptoteCalculationStep step = new(calculatorMock.Object, differentiation, 1e-3);

        GenericMechanicalModelInput input = new()
        {
            MechanicalModelName = "TestModel",
            TimeStep = 0.05,
            ConstitutiveParameters = new FungConstitutiveParameters(100.0, 1.0, new ReducedRelaxationFunction(0.1, 0.01, 10.0))
        };

        // Act
        (double asymptoteTime, double asymptoteStress) = step.Execute(input);

        // Assert
        // For tau = 5.0, exp(-t/5) <= 0.001 -> t >= 5 * ln(1000) ~ 34.54 s
        Assert.True(asymptoteTime >= 30.0 && asymptoteTime <= 40.0, $"Expected asymptoteTime ~ 34.54, got {asymptoteTime}");
        Assert.True(Math.Abs(asymptoteStress - 1.0) <= 0.01, $"Expected asymptoteStress ~ 1.0, got {asymptoteStress}");
    }

    [Fact]
    public void Execute_Should_RespectUserInformedTolerance()
    {
        // Arrange
        Mock<IMechanicalModelCalculatorFacade> calculatorMock = new();
        calculatorMock
            .Setup(c => c.CalculateStress(It.IsAny<GenericMechanicalModelInput>(), It.IsAny<double>(), It.IsAny<double?>()))
            .Returns<GenericMechanicalModelInput, double, double?>((_, time, _) =>
            {
                // Decays from 10.0 at t=0 to 2.0 as t -> infty with time constant tau = 10.0
                return 2.0 + 8.0 * Math.Exp(-time / 10.0);
            });

        // User informs a tighter tolerance: 1e-4
        const double userInformedTolerance = 1e-4;
        Differentiation differentiation = new();
        AsymptoteCalculationStep step = new(calculatorMock.Object, differentiation, userInformedTolerance);

        GenericMechanicalModelInput input = new()
        {
            MechanicalModelName = "TestModel",
            TimeStep = 0.1,
            ConstitutiveParameters = new FungConstitutiveParameters(100.0, 1.0, new ReducedRelaxationFunction(0.1, 0.01, 10.0))
        };

        // Act
        (double asymptoteTime, double asymptoteStress) = step.Execute(input);

        // Assert
        // Tighter tolerance 1e-4
        Assert.True(asymptoteTime >= 85.0 && asymptoteTime <= 100.0, $"Expected asymptoteTime ~ 92.10, got {asymptoteTime}");
        Assert.True(Math.Abs(asymptoteStress - 2.0) <= 0.002, $"Expected asymptoteStress ~ 2.0 with tighter tolerance, got {asymptoteStress}");
    }

    [Fact]
    public void Constructor_Should_InitializeSuccessfully_WhenParametersProvided()
    {
        // Arrange
        Mock<IMechanicalModelCalculatorFacade> calculatorMock = new();
        Differentiation differentiation = new();

        // Act
        AsymptoteCalculationStep step = new(calculatorMock.Object, differentiation, 2e-4);

        // Assert
        Assert.NotNull(step);
        Assert.Equal(nameof(AsymptoteCalculationStep), step.Name);
    }
}
