using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;

namespace UnitTests.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;

/// <summary>
/// Unit tests for <see cref="ExecuteParametricSweepValidator"/> verifying boundary and rule validation.
/// </summary>
public sealed class ExecuteParametricSweepValidatorTests
{
    private readonly ExecuteParametricSweepValidator _validator = new();

    /// <summary>
    /// Verifies that validating a null request returns a bad request.
    /// </summary>
    [Fact]
    public void Validate_WhenRequestIsNull_ReturnsBadRequest()
    {
        // Act
        Result result = _validator.Validate(null!);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        Assert.Contains("Parametric sweep request cannot be null.", result.Messages);
    }

    /// <summary>
    /// Verifies that omitting required parameters (model name, baseline) triggers validation errors.
    /// </summary>
    [Fact]
    public void Validate_WhenRequiredParametersMissing_ReturnsBadRequest()
    {
        // Arrange
        ExecuteParametricSweepRequest request = new()
        {
            MechanicalModelName = string.Empty,
            Baseline = null!,
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            TimeStep = 0.1,
            InitialTime = 0.0,
            FinalTime = 10.0,
            AsymptoteTolerance = 1e-4,
            ConstantVariables = new Dictionary<string, RangeParameters>
            {
                ["Stiffness"] = new RangeParameters { InitialPoint = 10.0, FinalPoint = 20.0, Step = 5.0 }
            }
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("MechanicalModelName is required.", result.Messages);
        Assert.Contains("Baseline constitutive parameters are required.", result.Messages);
    }

    /// <summary>
    /// Verifies that invalid temporal ranges and non-positive time steps trigger validation errors.
    /// </summary>
    [Fact]
    public void Validate_WhenTemporalParametersInvalid_ReturnsBadRequest()
    {
        // Arrange
        ExecuteParametricSweepRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            TimeStep = 0.0,
            InitialTime = 10.0,
            FinalTime = 5.0,
            AsymptoteTolerance = -1.0,
            ConstantVariables = new Dictionary<string, RangeParameters>
            {
                ["Stiffness"] = new RangeParameters { InitialPoint = 10.0, FinalPoint = 20.0, Step = 5.0 }
            }
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("TimeStep must be greater than zero.", result.Messages);
        Assert.Contains("FinalTime must be greater than InitialTime.", result.Messages);
        Assert.Contains("AsymptoteTolerance must be greater than zero.", result.Messages);
    }

    /// <summary>
    /// Verifies that omitting all swept variable sources triggers a validation error.
    /// </summary>
    [Fact]
    public void Validate_WhenNoSweptVariablesProvided_ReturnsBadRequest()
    {
        // Arrange
        ExecuteParametricSweepRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            TimeStep = 0.1,
            InitialTime = 0.0,
            FinalTime = 10.0,
            AsymptoteTolerance = 1e-4,
            ConstantVariables = [],
            ArrayVariables = [],
            StrainRange = null
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("At least one swept variable (ConstantVariables, ArrayVariables, or StrainRange) must be provided.", result.Messages);
    }

    /// <summary>
    /// Verifies that a valid parametric sweep request passes validation.
    /// </summary>
    [Fact]
    public void Validate_WhenValid_ReturnsSuccess()
    {
        // Arrange
        ExecuteParametricSweepRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            TimeStep = 0.1,
            InitialTime = 0.0,
            FinalTime = 10.0,
            AsymptoteTolerance = 1e-4,
            StrainRange = new RangeParameters { InitialPoint = 0.01, FinalPoint = 0.05, Step = 0.01 }
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.Empty(result.Messages);
    }
}
