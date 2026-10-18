using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.SensitivityAnalyses;

public sealed class StressCalculationStepTests
{
    [Fact]
    public void InitialStressCalculationStep_Execute_ShouldCallCalculatorWithInitialTime()
    {
        // Arrange
        Mock<IMechanicalModelCalculatorFacade> calculatorMock = new();
        calculatorMock
            .Setup(c => c.CalculateStress(It.IsAny<GenericMechanicalModelInput>(), 2.5, It.IsAny<double?>()))
            .Returns(42.0);

        InitialStressCalculationStep step = new(calculatorMock.Object, 2.5);
        GenericMechanicalModelInput input = new()
        {
            MechanicalModelName = "TestModel",
            ConstitutiveParameters = new FungConstitutiveParameters(100.0, 1.0, new ReducedRelaxationFunction(0.1, 0.01, 10.0))
        };

        // Act
        double stress = step.Execute(input);

        // Assert
        Assert.Equal(42.0, stress);
        calculatorMock.Verify(c => c.CalculateStress(input, 2.5, null), Times.Once);
    }

    [Fact]
    public void FinalStressCalculationStep_Execute_ShouldCallCalculatorWithFinalTime()
    {
        // Arrange
        Mock<IMechanicalModelCalculatorFacade> calculatorMock = new();
        calculatorMock
            .Setup(c => c.CalculateStress(It.IsAny<GenericMechanicalModelInput>(), 100.0, It.IsAny<double?>()))
            .Returns(18.5);

        FinalStressCalculationStep step = new(calculatorMock.Object, 100.0);
        GenericMechanicalModelInput input = new()
        {
            MechanicalModelName = "TestModel",
            ConstitutiveParameters = new FungConstitutiveParameters(100.0, 1.0, new ReducedRelaxationFunction(0.1, 0.01, 10.0))
        };

        // Act
        double stress = step.Execute(input);

        // Assert
        Assert.Equal(18.5, stress);
        calculatorMock.Verify(c => c.CalculateStress(input, 100.0, null), Times.Once);
    }
}
