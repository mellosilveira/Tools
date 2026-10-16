using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// Unit tests for <see cref="ParametricSweepPersistenceStep"/>, verifying database persistence of
/// <see cref="ParametricSweepEntity"/> and associated <see cref="StoredFileEntity"/> records.
/// </summary>
public sealed class ParametricSweepPersistenceStepTests
{
    private readonly Mock<IRepository> _repositoryMock = new();

    /// <summary>
    /// Verifies that ExecuteAsync persists both the sweep metadata entity and all generated file records,
    /// returning the external identifier on success.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenSuccessful_PersistsEntityAndFilesAndReturnsIdentifier()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StoredFileEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        ParametricSweepPersistenceStep step = new(
            NullLogger<ParametricSweepPersistenceStep>.Instance,
            _repositoryMock.Object);

        string identifier = "sweep_pers_01";
        ParametricSweepInput sweepInput = new()
        {
            MechanicalModelName = "Maxwell",
            Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
            ConstantVariables = new Dictionary<string, RangeParameters>
            {
                ["Stiffness"] = new RangeParameters { InitialPoint = 50.0, FinalPoint = 150.0, Step = 50.0 }
            },
            ArrayVariables = [],
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 10.0,
            TimeStep = 0.1,
            AsymptoteTolerance = 1e-5
        };

        FileMetadata[] generatedFiles =
        [
            new FileMetadata(@"C:\Data\SensitivityAnalysis\sweep_pers_01\Stiffness_SensitivityAnalysis.csv", 1024L, DateTimeOffset.UtcNow),
            new FileMetadata(@"C:\Data\SensitivityAnalysis\sweep_pers_01\Strain_SensitivityAnalysis.csv", 2048L, DateTimeOffset.UtcNow)
        ];

        ParametricSweepPersistenceInput input = new(identifier, sweepInput, generatedFiles);

        // Act
        string result = await step.ExecuteAsync(input);

        // Assert
        Assert.Equal(identifier, result);

        _repositoryMock.Verify(
            r => r.TryInsertAsync(
                It.Is<ParametricSweepEntity>(e =>
                    e.ExternalIdentifier == identifier &&
                    e.MechanicalModelName == "Maxwell" &&
                    e.BaseStrain == 0.05 &&
                    e.InitialTime == 0.0 &&
                    e.FinalTime == 10.0 &&
                    e.TimeStep == 0.1 &&
                    e.AsymptoteTolerance == 1e-5 &&
                    !string.IsNullOrWhiteSpace(e.BaselineParametersJson) &&
                    !string.IsNullOrWhiteSpace(e.VariableRangesJson)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            r => r.TryInsertAsync(
                It.Is<StoredFileEntity>(f =>
                    f.ExternalIdentifier == identifier &&
                    f.Context == "SensitivityAnalysis" &&
                    f.StorageProvider == StorageProvider.Local &&
                    f.ContentType == "text/csv" &&
                    f.SizeBytes > 0),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that when the sweep entity insertion encounters a conflict (already exists),
    /// the step logs a warning and proceeds to persist stored files without throwing.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenEntityInsertConflicts_LogsWarningAndPersistsStoredFiles()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateConflict(0L, "Entity already exists."));

        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StoredFileEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        ParametricSweepPersistenceStep step = new(
            NullLogger<ParametricSweepPersistenceStep>.Instance,
            _repositoryMock.Object);

        string identifier = "sweep_pers_conflict";
        ParametricSweepInput sweepInput = new()
        {
            MechanicalModelName = "Maxwell",
            Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
            ConstantVariables = [],
            ArrayVariables = [],
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 5.0,
            TimeStep = 0.1
        };

        FileMetadata[] generatedFiles =
        [
            new FileMetadata(@"C:\Data\SensitivityAnalysis\sweep_pers_conflict\Output.csv", 512L, DateTimeOffset.UtcNow)
        ];

        ParametricSweepPersistenceInput input = new(identifier, sweepInput, generatedFiles);

        // Act
        string result = await step.ExecuteAsync(input);

        // Assert
        Assert.Equal(identifier, result);
        _repositoryMock.Verify(r => r.TryInsertAsync(It.IsAny<StoredFileEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that when the sweep entity insertion fails with an error, an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenEntityInsertFails_ThrowsInvalidOperationException()
    {
        // Arrange
        Result<long> badRequestResult = Result.CreateBadRequest("Database constraint violation.");
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(badRequestResult);

        ParametricSweepPersistenceStep step = new(
            NullLogger<ParametricSweepPersistenceStep>.Instance,
            _repositoryMock.Object);

        ParametricSweepInput sweepInput = new()
        {
            MechanicalModelName = "Maxwell",
            Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
            ConstantVariables = [],
            ArrayVariables = [],
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 5.0,
            TimeStep = 0.1
        };

        ParametricSweepPersistenceInput input = new("sweep_fail", sweepInput, []);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => step.ExecuteAsync(input));
        Assert.Contains("Database constraint violation.", exception.Message);
    }

    /// <summary>
    /// Verifies that when stored file insertion fails, the step logs a warning without throwing and returns the identifier.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenStoredFileInsertFails_DoesNotThrowAndReturnsIdentifier()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        Result<long> badRequestResult = Result.CreateBadRequest("Disk error saving metadata.");
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StoredFileEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(badRequestResult);

        ParametricSweepPersistenceStep step = new(
            NullLogger<ParametricSweepPersistenceStep>.Instance,
            _repositoryMock.Object);

        ParametricSweepInput sweepInput = new()
        {
            MechanicalModelName = "Maxwell",
            Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
            ConstantVariables = [],
            ArrayVariables = [],
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 5.0,
            TimeStep = 0.1
        };

        FileMetadata[] generatedFiles =
        [
            new FileMetadata(@"C:\Data\SensitivityAnalysis\sweep_file_fail\Output.csv", 512L, DateTimeOffset.UtcNow)
        ];

        ParametricSweepPersistenceInput input = new("sweep_file_fail", sweepInput, generatedFiles);

        // Act
        string result = await step.ExecuteAsync(input);

        // Assert
        Assert.Equal("sweep_file_fail", result);
    }

    /// <summary>
    /// Verifies that DisposeAsync completes cleanly without errors.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_CompletesSuccessfully()
    {
        // Arrange
        ParametricSweepPersistenceStep step = new(
            NullLogger<ParametricSweepPersistenceStep>.Instance,
            _repositoryMock.Object);

        // Act & Assert
        await step.DisposeAsync();
    }
}
