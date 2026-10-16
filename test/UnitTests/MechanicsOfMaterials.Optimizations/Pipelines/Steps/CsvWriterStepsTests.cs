using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using Moq;
using System.Globalization;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.Steps;

public sealed class CsvWriterStepsTests : IDisposable
{
    private readonly string _testDirectory;

    public CsvWriterStepsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "CsvWriterTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors on temp files
            }
        }
    }

    [Fact]
    public async Task ExperimentalDataFileWriterStep_Should_WriteHeaderOnDispose_WhenEmpty()
    {
        // Arrange
        string filePath = Path.Combine(_testDirectory, "empty_exp.csv");
        IFileStorageService fileStorageService = new LocalFileStorageService();
        ExperimentalDataFileWriterStep step = new(fileStorageService, filePath, "empty_exp");

        // Act
        await step.DisposeAsync();

        // Assert
        Assert.True(File.Exists(filePath));
        string[] lines = await File.ReadAllLinesAsync(filePath);
        Assert.Single(lines);
        Assert.Equal("Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration", lines[0]);
    }

    [Fact]
    public async Task ExperimentalDataFileWriterStep_Should_WriteDataPointsAndTrackPaths()
    {
        // Arrange
        string filePath = Path.Combine(_testDirectory, "experimental.csv");
        IFileStorageService fileStorageService = new LocalFileStorageService();
        ExperimentalDataFileWriterStep step = new(fileStorageService, filePath, "experimental_01");

        SegmentedDataPoint dataPoint1 = new(
            SegmentType: SegmentType.Ramp,
            ProcessedDataPoint: new ProcessedDataPoint(0.0, 0.01, 0.1, 0.05, 100.5, 10.2, 1.1));

        SegmentedDataPoint dataPoint2 = new(
            SegmentType: SegmentType.Relaxation,
            ProcessedDataPoint: new ProcessedDataPoint(1.0, 0.02, 0.2, 0.06, 95.3, -5.2, -0.5));

        // Act
        Assert.Equal(filePath, step.FileMetadata.FullPath);

        await step.ExecuteAsync(dataPoint1);
        await step.ExecuteAsync(dataPoint2);
        await step.DisposeAsync();

        // Assert
        Assert.True(File.Exists(filePath));
        string[] lines = await File.ReadAllLinesAsync(filePath);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration", lines[0]);
        Assert.Equal(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6}", 0.0, 0.01, 0.1, 0.05, 100.5, 10.2, 1.1), lines[1]);
        Assert.Equal(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6}", 1.0, 0.02, 0.2, 0.06, 95.3, -5.2, -0.5), lines[2]);
        Assert.Equal(filePath, step.FileMetadata.FullPath);
        Assert.True(step.FileMetadata.SizeBytes > 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExperimentalDataFileWriterStep_Should_ThrowArgumentException_WhenIdentifierIsNullOrWhiteSpace(string identifier)
    {
        // Arrange
        string filePath = Path.Combine(_testDirectory, "invalid.csv");
        IFileStorageService fileStorageService = new LocalFileStorageService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ExperimentalDataFileWriterStep(fileStorageService, filePath, identifier));
    }

    [Fact]
    public async Task MechanicalModelOutputFileWriterStep_Should_SerializePointsAndReturnFileMetadata()
    {
        // Arrange
        IFileStorageService fileStorageService = new LocalFileStorageService();
        MechanicalModelOutputFileWriterStep step = new(fileStorageService, _testDirectory, "sim_01");

        MechanicalModelOutput[] points =
        [
            new MechanicalModelOutput { Time = 0.0, Stress = 10.0, Strain = 0.01 },
            new MechanicalModelOutput { Time = 1.0, Stress = 9.5, Strain = 0.01 }
        ];

        MechanicalModelSimulationPayload payload = new(
            CurveFit: new MechanicalModelCurveFitOutput(
                CurveSegments: [],
                MechanicalModelName: "Maxwell",
                ConstitutiveParameters: new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
                AcceptedRange: AcceptedRange.Default,
                LoadResponseRelationship: LoadResponseRelationship.StressStrain,
                ViscoelasticEffect: ViscoelasticEffect.Relaxation,
                RampTimeConsideration: RampTimeConsideration.Disregard,
                RSquared: 0.999,
                FinalError: 0.001,
                Iterations: 10),
            SimulatedPoints: points);

        // Act
        FileMetadata fileMetadata = await step.ExecuteAsync(payload);
        await step.DisposeAsync();

        // Assert
        Assert.Single(step.GeneratedFilePaths);
        string generatedPath = step.GeneratedFilePaths.First();
        Assert.Equal(generatedPath, fileMetadata.FullPath);
        Assert.True(File.Exists(generatedPath));
        Assert.True(fileMetadata.SizeBytes > 0);

        string[] lines = await File.ReadAllLinesAsync(generatedPath);
        Assert.True(lines.Length >= 3);
        Assert.StartsWith("Time,", lines[0]);
    }

    [Fact]
    public async Task ParametricSweepCsvWriterStep_Should_SegregateByVariableAndTrackAllPaths()
    {
        // Arrange
        string sweepDir = Path.Combine(_testDirectory, "SweepResults");
        ParametricSweepCsvWriterStep step = new(sweepDir);

        ParametricSweepFinalPayload payloadE = new(
            VariableName: "ElasticModulus",
            VariableValue: 150.0,
            InitialStress: 10.0,
            FinalStress: 5.0,
            AsymptoteTime: 12.5,
            AsymptoteStress: 4.8,
            AbsoluteDeltaStress: -5.0,
            PercentageDeltaStress: -50.0);

        ParametricSweepFinalPayload payloadTau = new(
            VariableName: "RelaxationTime",
            VariableValue: 2.5,
            InitialStress: 10.0,
            FinalStress: 6.0,
            AsymptoteTime: 8.0,
            AsymptoteStress: 5.5,
            AbsoluteDeltaStress: -4.0,
            PercentageDeltaStress: -40.0);

        // Act
        await step.ExecuteAsync(payloadE);
        await step.ExecuteAsync(payloadTau);
        await step.DisposeAsync();

        // Assert
        IReadOnlyCollection<string> generatedPaths = step.GeneratedFilePaths;
        Assert.Equal(2, generatedPaths.Count);

        string expectedPathE = Path.Combine(sweepDir, "ElasticModulus_SensitivityAnalysis.csv");
        string expectedPathTau = Path.Combine(sweepDir, "RelaxationTime_SensitivityAnalysis.csv");

        Assert.Contains(expectedPathE, generatedPaths);
        Assert.Contains(expectedPathTau, generatedPaths);
        Assert.True(File.Exists(expectedPathE));
        Assert.True(File.Exists(expectedPathTau));

        string[] linesE = await File.ReadAllLinesAsync(expectedPathE);
        Assert.Equal(2, linesE.Length);
        Assert.Equal("VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress", linesE[0]);
        Assert.StartsWith("150,10,5,12.5,4.8,-5,-50", linesE[1]);

        string[] linesTau = await File.ReadAllLinesAsync(expectedPathTau);
        Assert.Equal(2, linesTau.Length);
        Assert.Equal("VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress", linesTau[0]);
        Assert.StartsWith("2.5,10,6,8,5.5,-4,-40", linesTau[1]);
    }
}
