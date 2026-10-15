using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.SensitivityAnalysis;

/// <summary>
/// End-to-end acceptance tests for <see cref="ParametricSweepPipeline"/> validating 1D sensitivity analysis
/// across constitutive models, confirming numerical stability and partitioned CSV outputs.
/// </summary>
public sealed class ParametricSweepPipelineAcceptanceTests : IDisposable
{
    private readonly string _testOutputDirectory;

    public ParametricSweepPipelineAcceptanceTests()
    {
        _testOutputDirectory = Path.Combine(AppContext.BaseDirectory, "Acceptance_Sensitivity_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testOutputDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDirectory))
        {
            try
            {
                Directory.Delete(_testOutputDirectory, true);
            }
            catch
            {
                // Suppress cleanup failures on temporary test artifacts
            }
        }
    }

    [Fact]
    public async Task ProcessAsync_FungModel_ShouldExecuteSensitivityAnalysisAndGenerateValidCsvs()
    {
        // Arrange
        using IServiceScope scope = GlobalServiceProvider.Provider.CreateScope();
        ParametricSweepPipeline pipeline = scope.ServiceProvider.GetRequiredService<ParametricSweepPipeline>();

        FungConstitutiveParameters baseline = new(
            elasticStressConstant: 50.0,
            elasticPowerConstant: 1.5,
            reducedRelaxationFunction: new ReducedRelaxationFunction(0.12, 0.02, 20.0));

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            ["c"] = new RangeParameters { InitialPoint = 0.05, FinalPoint = 0.15, Step = 0.05 },
            ["tau1"] = new RangeParameters { InitialPoint = 0.01, FinalPoint = 0.03, Step = 0.01 }
        };

        ParametricSweepInput input = new()
        {
            Identifier = "Acceptance_Fung_Sweep",
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.04,
            StrainRange = new RangeParameters { InitialPoint = 0.02, FinalPoint = 0.06, Step = 0.02 },
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 10.0,
            TimeStep = 0.1,
            OutputDirectory = _testOutputDirectory
        };

        // Act
        Result<string> result = await pipeline.ProcessAsync(input);

        // Assert
        Assert.True(result.Success, result.Messages.FirstOrDefault());
        Assert.False(string.IsNullOrWhiteSpace(result.Data));

        string cCsv = Path.Combine(_testOutputDirectory, "c_SensitivityAnalysis.csv");
        string tau1Csv = Path.Combine(_testOutputDirectory, "tau1_SensitivityAnalysis.csv");
        string strainCsv = Path.Combine(_testOutputDirectory, "Strain_SensitivityAnalysis.csv");

        Assert.True(File.Exists(cCsv), $"Expected {cCsv} to exist.");
        Assert.True(File.Exists(tau1Csv), $"Expected {tau1Csv} to exist.");
        Assert.True(File.Exists(strainCsv), $"Expected {strainCsv} to exist.");

        // Validate c CSV content (header + 3 values: 0.05, 0.10, 0.15)
        string[] cLines = await File.ReadAllLinesAsync(cCsv);
        Assert.Equal(4, cLines.Length);
        Assert.Equal("VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress", cLines[0]);
        ValidateCsvDataRows(cLines.Skip(1));

        // Validate tau1 CSV content (header + 3 values: 0.01, 0.02, 0.03)
        string[] tau1Lines = await File.ReadAllLinesAsync(tau1Csv);
        Assert.Equal(4, tau1Lines.Length);
        ValidateCsvDataRows(tau1Lines.Skip(1));

        // Validate Strain CSV content (header + 3 values: 0.02, 0.04, 0.06)
        string[] strainLines = await File.ReadAllLinesAsync(strainCsv);
        Assert.Equal(4, strainLines.Length);
        ValidateCsvDataRows(strainLines.Skip(1));
    }

    [Fact]
    public async Task ProcessAsync_SimplifiedFungModel_ShouldExecuteSensitivityAnalysisAndGenerateValidCsvs()
    {
        // Arrange
        using IServiceScope scope = GlobalServiceProvider.Provider.CreateScope();
        ParametricSweepPipeline pipeline = scope.ServiceProvider.GetRequiredService<ParametricSweepPipeline>();

        PronySeries pronySeries = new(0.4, [0.5, -0.1]);
        SimplifiedFungConstitutiveParameters baseline = new(
            elasticStressConstant: 80.0,
            elasticPowerConstant: 2.0,
            reducedRelaxationFunction: pronySeries);

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            ["ElasticStressConstant"] = new RangeParameters { InitialPoint = 60.0, FinalPoint = 100.0, Step = 20.0 }
        };

        ParametricSweepInput input = new()
        {
            Identifier = "Acceptance_SimplifiedFung_Sweep",
            MechanicalModelName = nameof(MechanicalModel.SimplifiedFung),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.05,
            StrainRange = new RangeParameters { InitialPoint = 0.03, FinalPoint = 0.07, Step = 0.02 },
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 5.0,
            TimeStep = 0.1,
            OutputDirectory = _testOutputDirectory
        };

        // Act
        Result<string> result = await pipeline.ProcessAsync(input);

        // Assert
        Assert.True(result.Success, result.Messages.FirstOrDefault());

        string stressConstCsv = Path.Combine(_testOutputDirectory, "ElasticStressConstant_SensitivityAnalysis.csv");
        string strainCsv = Path.Combine(_testOutputDirectory, "Strain_SensitivityAnalysis.csv");

        Assert.True(File.Exists(stressConstCsv), $"Expected {stressConstCsv} to exist.");
        Assert.True(File.Exists(strainCsv), $"Expected {strainCsv} to exist.");

        string[] stressLines = await File.ReadAllLinesAsync(stressConstCsv);
        Assert.Equal(4, stressLines.Length);
        ValidateCsvDataRows(stressLines.Skip(1));

        string[] strainLines = await File.ReadAllLinesAsync(strainCsv);
        Assert.Equal(4, strainLines.Length);
        ValidateCsvDataRows(strainLines.Skip(1));
    }

    [Fact]
    public async Task ProcessAsync_SchaperyModel_ShouldExecuteSensitivityAnalysisAndGenerateValidCsvs()
    {
        // Arrange
        using IServiceScope scope = GlobalServiceProvider.Provider.CreateScope();
        ParametricSweepPipeline pipeline = scope.ServiceProvider.GetRequiredService<ParametricSweepPipeline>();

        SchaperyConstitutiveParameters baseline = new()
        {
            Ge = 15.0,
            G0 = 1.0,
            G1 = 1.0,
            G2 = 1.0,
            He = new PolynomialFunction([1.0]),
            H1 = new PolynomialFunction([1.0]),
            H2 = new PolynomialFunction([1.0]),
            TransientRelaxationFunction = new PowerLaw([5.0, 0.2])
        };

        Dictionary<string, RangeParameters> constantVariables = new()
        {
            ["Ge"] = new RangeParameters { InitialPoint = 10.0, FinalPoint = 20.0, Step = 5.0 },
            ["g1"] = new RangeParameters { InitialPoint = 4.0, FinalPoint = 6.0, Step = 1.0 }
        };

        ParametricSweepInput input = new()
        {
            Identifier = "Acceptance_Schapery_Sweep",
            MechanicalModelName = nameof(MechanicalModel.Schapery),
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = [],
            BaseStrain = 0.05,
            StrainRange = new RangeParameters { InitialPoint = 0.03, FinalPoint = 0.05, Step = 0.02 },
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.5,
            FinalTime = 10.0,
            TimeStep = 0.1,
            OutputDirectory = _testOutputDirectory
        };

        // Act
        Result<string> result = await pipeline.ProcessAsync(input);

        // Assert
        Assert.True(result.Success, result.Messages.FirstOrDefault());

        string geCsv = Path.Combine(_testOutputDirectory, "Ge_SensitivityAnalysis.csv");
        string g1Csv = Path.Combine(_testOutputDirectory, "g1_SensitivityAnalysis.csv");
        string strainCsv = Path.Combine(_testOutputDirectory, "Strain_SensitivityAnalysis.csv");

        Assert.True(File.Exists(geCsv), $"Expected {geCsv} to exist.");
        Assert.True(File.Exists(g1Csv), $"Expected {g1Csv} to exist.");
        Assert.True(File.Exists(strainCsv), $"Expected {strainCsv} to exist.");

        string[] geLines = await File.ReadAllLinesAsync(geCsv);
        Assert.Equal(4, geLines.Length);
        ValidateCsvDataRows(geLines.Skip(1));

        string[] g1Lines = await File.ReadAllLinesAsync(g1Csv);
        Assert.Equal(4, g1Lines.Length);
        ValidateCsvDataRows(g1Lines.Skip(1));

        string[] strainLines = await File.ReadAllLinesAsync(strainCsv);
        Assert.Equal(3, strainLines.Length);
        ValidateCsvDataRows(strainLines.Skip(1));
    }

    private static void ValidateCsvDataRows(IEnumerable<string> rows)
    {
        foreach (string row in rows)
        {
            string[] parts = row.Split(',');
            Assert.Equal(7, parts.Length);

            double variableVal = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            double initialStress = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            double finalStress = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
            double asymptoteTime = double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
            double asymptoteStress = double.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture);
            double absDelta = double.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture);
            double pctDelta = double.Parse(parts[6], System.Globalization.CultureInfo.InvariantCulture);

            Assert.False(double.IsNaN(variableVal));
            Assert.False(double.IsNaN(initialStress));
            Assert.False(double.IsNaN(finalStress));
            Assert.False(double.IsNaN(asymptoteTime));
            Assert.False(double.IsNaN(asymptoteStress));
            Assert.False(double.IsNaN(absDelta));
            Assert.False(double.IsNaN(pctDelta));

            Assert.True(initialStress > 0.0, $"Initial stress must be positive, got {initialStress}");
            Assert.True(finalStress > 0.0, $"Final stress must be positive, got {finalStress}");
            Assert.True(finalStress <= initialStress, $"Final stress should be less than or equal to initial stress during relaxation, got initial={initialStress}, final={finalStress}");
            Assert.True(asymptoteTime >= 0.0, $"Asymptote time must be non-negative, got {asymptoteTime}");
            Assert.Equal(finalStress - initialStress, absDelta, 6);
            Assert.InRange(pctDelta, 0.0, 100.0);
        }
    }
}
