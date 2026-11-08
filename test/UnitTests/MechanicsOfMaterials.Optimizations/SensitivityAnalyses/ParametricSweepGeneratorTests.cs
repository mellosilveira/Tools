using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;

namespace UnitTests.MechanicsOfMaterials.Optimizations.SensitivityAnalyses;

public sealed class ParametricSweepGeneratorTests
{
    [Fact]
    public void FungParametricSweepGenerator_GenerateCombinations_ShouldYieldStrainAndConstantVariablesWithoutDuplicates()
    {
        // Arrange
        FungParametricSweepGeneratorStep generator = new();
        FungConstitutiveParameters baseline = new(
            elasticStressConstant: 1000.0,
            elasticPowerConstant: 1.0,
            reducedRelaxationFunction: new ReducedRelaxationFunction(0.1, 0.01, 10.0));

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            [nameof(FungConstitutiveParameters.ElasticStressConstant)] = new RangeParameters { InitialPoint = 100.0, FinalPoint = 300.0, Step = 100.0 },
            [nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness)] = new RangeParameters { InitialPoint = 0.05, FinalPoint = 0.15, Step = 0.05 }
        };

        ParametricSweepPipelineInput input = new()
        {
            Identifier = "test_fung_gen",
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.05,
            StrainRange = new RangeParameters { InitialPoint = 0.01, FinalPoint = 0.03, Step = 0.01 },
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 10.0,
            TimeStep = 0.1,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(0.0, 1000.0, 1e-3, 1000)
        };

        // Act
        List<(string VariableName, List<ParametricSweepInput> Inputs)> groups = generator
            .GenerateCombinations(input)
            .Select(g => (g.VariableName, g.Inputs.ToList()))
            .ToList();

        // Assert
        Assert.Equal(3, groups.Count);
        Assert.Equal("Strain", groups[0].VariableName);
        Assert.Equal(3, groups[0].Inputs.Count);

        Assert.Equal(nameof(FungConstitutiveParameters.ElasticStressConstant), groups[1].VariableName);
        Assert.Equal(3, groups[1].Inputs.Count);

        Assert.Equal(nameof(FungConstitutiveParameters.ReducedRelaxationFunction.RelaxationStiffness), groups[2].VariableName);
        Assert.Equal(3, groups[2].Inputs.Count);
    }

    [Fact]
    public void SimplifiedFungParametricSweepGenerator_GenerateCombinations_ShouldYieldElasticAndIndexedArrayVariables()
    {
        // Arrange
        SimplifiedFungParametricSweepGeneratorStep generator = new();
        SimplifiedFungConstitutiveParameters baseline = new(
            elasticStressConstant: 500.0,
            elasticPowerConstant: 2.0,
            reducedRelaxationFunction: new PronySeries(10.0, [1.0, 0.5]));

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            [nameof(SimplifiedFungConstitutiveParameters.ElasticStressConstant)] = new RangeParameters { InitialPoint = 400.0, FinalPoint = 600.0, Step = 200.0 }
        };

        Dictionary<string, Dictionary<int, RangeParameters>> arrayVariables = new()
        {
            [nameof(SimplifiedFungConstitutiveParameters.ReducedRelaxationFunction)] = new Dictionary<int, RangeParameters>
            {
                [1] = new RangeParameters { InitialPoint = 0.5, FinalPoint = 1.5, Step = 0.5 }
            }
        };

        ParametricSweepPipelineInput input = new()
        {
            Identifier = "test_sfung_gen",
            MechanicalModelName = nameof(MechanicalModel.SimplifiedFung),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = arrayVariables,
            BaseStrain = 0.04,
            StrainRange = null,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 5.0,
            TimeStep = 0.1,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(0.0, 500.0, 1e-3, 500)
        };

        // Act
        List<(string VariableName, List<ParametricSweepInput> Inputs)> groups = generator
            .GenerateCombinations(input)
            .Select(g => (g.VariableName, g.Inputs.ToList()))
            .ToList();

        // Assert
        Assert.Equal(2, groups.Count);
        Assert.Equal(nameof(SimplifiedFungConstitutiveParameters.ElasticStressConstant), groups[0].VariableName);
        Assert.Equal(2, groups[0].Inputs.Count);

        Assert.Equal("ReducedRelaxationFunction[1]", groups[1].VariableName);
        Assert.Equal(3, groups[1].Inputs.Count);
        Assert.All(groups[1].Inputs, item => Assert.Equal("ReducedRelaxationFunction[1]", item.VariableName));
    }

    [Fact]
    public void SchaperyParametricSweepGenerator_GenerateCombinations_ShouldYieldScalarAndPowerLawVariables()
    {
        // Arrange
        SchaperyParametricSweepGeneratorStep generator = new();
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
            [$"{nameof(SchaperyConstitutiveParameters.TransientRelaxationFunction)}.C"] = new RangeParameters { InitialPoint = 1.0, FinalPoint = 3.0, Step = 1.0 }
        };

        ParametricSweepPipelineInput input = new()
        {
            Identifier = "test_schapery_gen",
            MechanicalModelName = nameof(MechanicalModel.Schapery),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.03,
            StrainRange = null,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 20.0,
            TimeStep = 0.5,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(0.0, 1000.0, 1e-3, 1000)
        };

        // Act
        List<ParametricSweepInput> envelopes = generator
            .GenerateCombinations(input)
            .SelectMany(g => g.Inputs)
            .ToList();

        // Assert
        Assert.Equal(6, envelopes.Count);
        ParametricSweepInput powerLawCEnvelope = envelopes.First(e => e.VariableName == "TransientRelaxationFunction.C" && e.VariableValue == 3.0);
        SchaperyConstitutiveParameters mutatedParams = Assert.IsType<SchaperyConstitutiveParameters>(powerLawCEnvelope.SimulationInput.ConstitutiveParameters);
        Assert.NotNull(mutatedParams.TransientRelaxationFunction);
        Assert.Equal(3.0, mutatedParams.TransientRelaxationFunction.Coefficients[0]);
    }
}
