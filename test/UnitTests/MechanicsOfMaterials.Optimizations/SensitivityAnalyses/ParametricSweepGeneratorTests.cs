using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace UnitTests.MechanicsOfMaterials.Optimizations.SensitivityAnalyses;

public sealed class ParametricSweepGeneratorTests
{
    [Fact]
    public void FungParametricSweepGenerator_GenerateCombinations_ShouldYieldStrainAndConstantVariables()
    {
        // Arrange
        FungParametricSweepGenerator generator = new();
        FungConstitutiveParameters baseline = new(
            elasticStressConstant: 1000.0,
            elasticPowerConstant: 1.0,
            reducedRelaxationFunction: new ReducedRelaxationFunction(0.1, 0.01, 10.0));

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            [nameof(FungConstitutiveParameters.ElasticStressConstant)] = new RangeParameters { InitialPoint = 100.0, FinalPoint = 300.0, Step = 100.0 },
            [nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness)] = new RangeParameters { InitialPoint = 0.05, FinalPoint = 0.15, Step = 0.05 }
        };

        ParametricSweepInput input = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.05,
            StrainRange = new RangeParameters { InitialPoint = 0.01, FinalPoint = 0.03, Step = 0.01 },
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 10.0,
            TimeStep = 0.1
        };

        // Act
        List<SweepSimulationEnvelope> envelopes = generator.GenerateCombinations(input).ToList();

        // Assert
        Assert.NotEmpty(envelopes);
        Assert.Contains(envelopes, e => e.VariableName == "Strain");
        Assert.Contains(envelopes, e => e.VariableName == nameof(FungConstitutiveParameters.ElasticStressConstant));
        Assert.Contains(envelopes, e => e.VariableName == nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness));

        // Strain values: 0.01, 0.02, 0.03 (3)
        // ElasticStressConstant values: 100, 200, 300 (3)
        // RelaxationStiffness values: 0.05, 0.10, 0.15 (3)
        // Total = 9
        Assert.Equal(9, envelopes.Count);
    }

    [Fact]
    public void ParametricSweepGeneratorBase_GenerateCombinations_ShouldHandleNullStrainRangeGracefully()
    {
        // Arrange
        FungParametricSweepGenerator generator = new();
        FungConstitutiveParameters baseline = new(
            elasticStressConstant: 1000.0,
            elasticPowerConstant: 1.0,
            reducedRelaxationFunction: new ReducedRelaxationFunction(0.1, 0.01, 10.0));

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            [nameof(FungConstitutiveParameters.ElasticStressConstant)] = new RangeParameters { InitialPoint = 100.0, FinalPoint = 200.0, Step = 100.0 }
        };

        ParametricSweepInput input = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.05,
            StrainRange = null,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 10.0,
            TimeStep = 0.1
        };

        // Act
        List<SweepSimulationEnvelope> envelopes = generator.GenerateCombinations(input).ToList();

        // Assert
        Assert.NotEmpty(envelopes);
        Assert.DoesNotContain(envelopes, e => e.VariableName == "Strain");
        Assert.Equal(2, envelopes.Count);
    }

    [Fact]
    public void SimplifiedFungParametricSweepGenerator_GenerateCombinations_ShouldYieldElasticVariables()
    {
        // Arrange
        SimplifiedFungParametricSweepGenerator generator = new();
        SimplifiedFungConstitutiveParameters baseline = new(
            elasticStressConstant: 500.0,
            elasticPowerConstant: 2.0,
            reducedRelaxationFunction: new PronySeries(10.0, [1.0, 0.5]));

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            [nameof(SimplifiedFungConstitutiveParameters.ElasticStressConstant)] = new RangeParameters { InitialPoint = 400.0, FinalPoint = 600.0, Step = 200.0 },
            [nameof(SimplifiedFungConstitutiveParameters.ElasticPowerConstant)] = new RangeParameters { InitialPoint = 1.0, FinalPoint = 3.0, Step = 1.0 }
        };

        ParametricSweepInput input = new()
        {
            MechanicalModelName = nameof(MechanicalModel.SimplifiedFung),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.04,
            StrainRange = null,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 5.0,
            TimeStep = 0.1
        };

        // Act
        List<SweepSimulationEnvelope> envelopes = generator.GenerateCombinations(input).ToList();

        // Assert
        Assert.Equal(5, envelopes.Count); // ElasticStressConstant (2) + ElasticPowerConstant (3)
        Assert.Contains(envelopes, e => e.VariableName == nameof(SimplifiedFungConstitutiveParameters.ElasticStressConstant));
        Assert.Contains(envelopes, e => e.VariableName == nameof(SimplifiedFungConstitutiveParameters.ElasticPowerConstant));
    }

    [Fact]
    public void SchaperyParametricSweepGenerator_GenerateCombinations_ShouldYieldScalarAndPowerLawVariables()
    {
        // Arrange
        SchaperyParametricSweepGenerator generator = new();
        SchaperyConstitutiveParameters baseline = new()
        {
            Ge = 10.0,
            G0 = 1.0,
            G1 = 1.0,
            G2 = 1.0,
            J0 = 0.1,
            TransientRelaxationFunction = new PowerLaw([2.0, 0.1])
        };

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            [nameof(SchaperyConstitutiveParameters.Ge)] = new RangeParameters { InitialPoint = 8.0, FinalPoint = 12.0, Step = 2.0 },
            ["g1"] = new RangeParameters { InitialPoint = 1.0, FinalPoint = 3.0, Step = 1.0 },
            ["n"] = new RangeParameters { InitialPoint = 0.05, FinalPoint = 0.15, Step = 0.05 }
        };

        ParametricSweepInput input = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Schapery),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.03,
            StrainRange = null,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 20.0,
            TimeStep = 0.5
        };

        // Act
        List<SweepSimulationEnvelope> envelopes = generator.GenerateCombinations(input).ToList();

        // Assert
        Assert.NotEmpty(envelopes);
        Assert.Contains(envelopes, e => e.VariableName == nameof(SchaperyConstitutiveParameters.Ge));
        Assert.Contains(envelopes, e => e.VariableName == "g1");
        Assert.Contains(envelopes, e => e.VariableName == "n");

        // Verify that g1 mutated correctly in the constitutive parameters
        SweepSimulationEnvelope g1Envelope = envelopes.First(e => e.VariableName == "g1" && e.VariableValue == 3.0);
        SchaperyConstitutiveParameters g1Params = Assert.IsType<SchaperyConstitutiveParameters>(g1Envelope.SimulationInput.ConstitutiveParameters);
        Assert.NotNull(g1Params.TransientRelaxationFunction);
        Assert.Equal(3.0, g1Params.TransientRelaxationFunction.Coefficients[0]);
    }
}
