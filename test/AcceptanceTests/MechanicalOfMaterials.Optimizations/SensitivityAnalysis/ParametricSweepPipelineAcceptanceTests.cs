using AcceptanceTests.MechanicalOfMaterials.Optimizations.SensitivityAnalysis.Serialization;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.SensitivityAnalysis;

/// <summary>
/// End-to-end acceptance tests for <see cref="ParametricSweepPipeline"/> validating 1D sensitivity analysis
/// driven dynamically by JSON specification files located in the TestData directory.
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

    /// <summary>
    /// Enumerates all JSON input files located in the TestData folder.
    /// </summary>
    /// <returns>A collection of file paths wrapped in <see cref="TheoryData{T}"/>.</returns>
    public static TheoryData<string> GetJsonInputFiles()
    {
        TheoryData<string> data = new();
        string testDataDir = ResolveTestDataDirectory();
        string[] jsonFiles = Directory.GetFiles(testDataDir, "*.json", SearchOption.TopDirectoryOnly);

        foreach (string filePath in jsonFiles.OrderBy(f => f))
        {
            data.Add(filePath);
        }

        return data;
    }

    /// <summary>
    /// Executes the parametric sweep pipeline using configurations defined in JSON files,
    /// verifying numerical stability, convergence, and physical validity of generated CSV outputs.
    /// </summary>
    /// <param name="jsonFilePath">The path to the JSON input file.</param>
    [Theory]
    [MemberData(nameof(GetJsonInputFiles))]
    public async Task ProcessAsync_FromJsonFile_ShouldExecuteSensitivityAnalysisAndGenerateValidCsvs(string jsonFilePath)
    {
        // Arrange
        using IServiceScope scope = GlobalServiceProvider.Provider.CreateScope();
        ParametricSweepPipeline pipeline = scope.ServiceProvider.GetRequiredService<ParametricSweepPipeline>();
        MelloSilveiraTools.Core.Managers.File.Providers.IFileStorageService fileStorageService = scope.ServiceProvider.GetRequiredService<MelloSilveiraTools.Core.Managers.File.Providers.IFileStorageService>();

        ParametricSweepInput input = ParametricSweepInputJsonParser.Parse(File.ReadAllText(jsonFilePath));

        // Act
        Result<string> result = await pipeline.ProcessAsync(input);

        // Assert
        Assert.True(result.Success, result.Messages.FirstOrDefault());
        Assert.False(string.IsNullOrWhiteSpace(result.Data));
        string identifier = result.Data!;

        // Validate each constant variable CSV output
        foreach (string variableName in input.ConstantVariables.Keys)
        {
            string csvFile = fileStorageService.ResolveLocation(identifier, "SensitivityAnalysis", $"{variableName}_SensitivityAnalysis.csv");
            Assert.True(File.Exists(csvFile), $"Expected {csvFile} to exist for variable '{variableName}'.");

            string[] lines = await File.ReadAllLinesAsync(csvFile);
            Assert.True(lines.Length > 1, $"Expected header and at least 1 data row in {csvFile}");
            Assert.Equal("VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress", lines[0]);
            ValidateCsvDataRows(lines.Skip(1));
        }

        // Validate Strain variation CSV output if configured
        if (input.StrainRange is not null)
        {
            string strainCsv = fileStorageService.ResolveLocation(identifier, "SensitivityAnalysis", "Strain_SensitivityAnalysis.csv");
            Assert.True(File.Exists(strainCsv), $"Expected {strainCsv} to exist for Strain variation.");

            string[] lines = await File.ReadAllLinesAsync(strainCsv);
            Assert.True(lines.Length > 1, $"Expected header and at least 1 data row in {strainCsv}");
            Assert.Equal("VariableValue,InitialStress,FinalStress,AsymptoteTime,AsymptoteStress,AbsoluteDeltaStress,PercentageDeltaStress", lines[0]);
            ValidateCsvDataRows(lines.Skip(1));
        }
    }

    private static string ResolveTestDataDirectory()
    {
        string baseDir = AppContext.BaseDirectory;
        string copiedDir = Path.Combine(baseDir, "MechanicalOfMaterials.Optimizations", "SensitivityAnalysis", "TestData");
        if (Directory.Exists(copiedDir))
        {
            return copiedDir;
        }

        string sourceDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "MechanicalOfMaterials.Optimizations", "SensitivityAnalysis", "TestData"));
        if (Directory.Exists(sourceDir))
        {
            return sourceDir;
        }

        throw new DirectoryNotFoundException($"Could not locate TestData directory at '{copiedDir}' or '{sourceDir}'.");
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
