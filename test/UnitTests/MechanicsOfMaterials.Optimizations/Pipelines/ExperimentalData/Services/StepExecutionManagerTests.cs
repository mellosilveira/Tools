using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Serialization;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Services;

/// <summary>
/// Unit tests for <see cref="StepExecutionManager"/> verifying checkpoint tracking, state transitions,
/// payload compression persistence, and rollback purging.
/// </summary>
public sealed class StepExecutionManagerTests
{
    private readonly Mock<ILogger<StepExecutionManager>> _loggerMock = new();
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task RecordStepPendingAsync_WhenStepDoesNotExist_InsertsNewEntityWithPendingState()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByUniqueColumnAsync<AnalysisStepExecutionEntity>("exp_1_2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisStepExecutionEntity?)null);

        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<AnalysisStepExecutionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        StepExecutionManager manager = new(_loggerMock.Object, _fileStorageServiceMock.Object, _repositoryMock.Object, _timeProvider);

        // Act
        await manager.RecordStepPendingAsync("exp_1", "CurveFitter", 2, new { Param = "test" });

        // Assert
        _repositoryMock.Verify(r => r.TryInsertAsync(
            It.Is<AnalysisStepExecutionEntity>(e => e.Identifier == "exp_1_2" && e.StepName == "CurveFitter" && e.State == PipelineStepState.Pending),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordStepStartedAsync_WhenStepDoesNotExist_InsertsNewEntity()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByUniqueColumnAsync<AnalysisStepExecutionEntity>("exp_1_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisStepExecutionEntity?)null);

        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<AnalysisStepExecutionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        StepExecutionManager manager = new(_loggerMock.Object, _fileStorageServiceMock.Object, _repositoryMock.Object, _timeProvider);

        // Act
        await manager.RecordStepStartedAsync("exp_1", "Segmenter", 1, new { Param = "test" });

        // Assert
        _repositoryMock.Verify(r => r.TryInsertAsync(
            It.Is<AnalysisStepExecutionEntity>(e => e.Identifier == "exp_1_1" && e.StepName == "Segmenter" && e.State == PipelineStepState.Running),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordStepCompletedAsync_WhenStepExists_UpdatesStateAndOutput()
    {
        // Arrange
        AnalysisStepExecutionEntity existing = new()
        {
            Id = 42,
            Identifier = "exp_1_2",
            AnalysisIdentifier = "exp_1",
            StepName = "CurveFitter",
            StepOrder = 2,
            State = PipelineStepState.Running,
            InputData = []
        };

        _repositoryMock
            .Setup(r => r.GetByUniqueColumnAsync<AnalysisStepExecutionEntity>("exp_1_2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<AnalysisStepExecutionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        StepExecutionManager manager = new(_loggerMock.Object, _fileStorageServiceMock.Object, _repositoryMock.Object, _timeProvider);

        // Act
        await manager.RecordStepCompletedAsync("exp_1", "CurveFitter", 2, new { RSquared = 0.99 }, 1);

        // Assert
        _repositoryMock.Verify(r => r.TryUpdateAsync(
            It.Is<AnalysisStepExecutionEntity>(e => e.Id == 42 && e.State == PipelineStepState.Completed && e.ProcessedItems == 1 && e.OutputData != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordStepFaultedAsync_WhenStepExists_UpdatesStateToFaulted()
    {
        // Arrange
        AnalysisStepExecutionEntity existing = new()
        {
            Id = 55,
            Identifier = "exp_1_3",
            AnalysisIdentifier = "exp_1",
            StepName = "Simulation",
            StepOrder = 3,
            State = PipelineStepState.Running,
            InputData = []
        };

        _repositoryMock
            .Setup(r => r.GetByUniqueColumnAsync<AnalysisStepExecutionEntity>("exp_1_3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<AnalysisStepExecutionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        StepExecutionManager manager = new(_loggerMock.Object, _fileStorageServiceMock.Object, _repositoryMock.Object, _timeProvider);

        // Act
        await manager.RecordStepFaultedAsync("exp_1", "Simulation", 3, "Integration divergence");

        // Assert
        _repositoryMock.Verify(r => r.TryUpdateAsync(
            It.Is<AnalysisStepExecutionEntity>(e => e.Id == 55 && e.State == PipelineStepState.Faulted && e.ErrorMessage == "Integration divergence"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordCurrentRunningStepFaultedAsync_WhenRunningStepExists_FaultsActiveRunningStep()
    {
        // Arrange
        AnalysisStepExecutionEntity step1 = new()
        {
            Id = 1,
            Identifier = "exp_1_1",
            AnalysisIdentifier = "exp_1",
            StepName = "Segmenter",
            StepOrder = 1,
            State = PipelineStepState.Completed,
            InputData = []
        };

        AnalysisStepExecutionEntity step2 = new()
        {
            Id = 2,
            Identifier = "exp_1_2",
            AnalysisIdentifier = "exp_1",
            StepName = "CurveFitter",
            StepOrder = 2,
            State = PipelineStepState.Running,
            InputData = []
        };

        AnalysisStepExecutionEntity step3 = new()
        {
            Id = 3,
            Identifier = "exp_1_3",
            AnalysisIdentifier = "exp_1",
            StepName = "Simulation",
            StepOrder = 3,
            State = PipelineStepState.Pending,
            InputData = []
        };

        _repositoryMock
            .Setup(r => r.GetAsync<AnalysisStepExecutionEntity, AnalysisStepExecutionFilter>(It.IsAny<AnalysisStepExecutionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable([step1, step2, step3]));

        _repositoryMock
            .Setup(r => r.GetByUniqueColumnAsync<AnalysisStepExecutionEntity>("exp_1_2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(step2);

        _repositoryMock
            .Setup(r => r.TryUpdateAsync(It.IsAny<AnalysisStepExecutionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        StepExecutionManager manager = new(_loggerMock.Object, _fileStorageServiceMock.Object, _repositoryMock.Object, _timeProvider);

        // Act
        await manager.RecordCurrentRunningStepFaultedAsync("exp_1", "Curve fit failed");

        // Assert
        _repositoryMock.Verify(r => r.TryUpdateAsync(
            It.Is<AnalysisStepExecutionEntity>(e => e.Id == 2 && e.StepName == "CurveFitter" && e.State == PipelineStepState.Faulted && e.ErrorMessage == "Curve fit failed"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackFromStepAsync_WhenOutputContainsLocation_DeletesPhysicalFileAndEntity()
    {
        // Arrange
        string fileToPurge = "outputs/sim_result.csv";
        byte[] compressedOutput = StepPayloadCompressor.Compress(new { SimulationOutputFileLocation = fileToPurge });

        AnalysisStepExecutionEntity step1 = new()
        {
            Id = 1,
            Identifier = "exp_1_1",
            AnalysisIdentifier = "exp_1",
            StepName = "Segmenter",
            StepOrder = 1,
            State = PipelineStepState.Completed,
            InputData = [],
            OutputData = StepPayloadCompressor.Compress(new { SegmentedFileLocation = "data/segmented.csv" })
        };

        AnalysisStepExecutionEntity step2 = new()
        {
            Id = 2,
            Identifier = "exp_1_2",
            AnalysisIdentifier = "exp_1",
            StepName = "CurveFitter",
            StepOrder = 2,
            State = PipelineStepState.Completed,
            InputData = [],
            OutputData = []
        };

        AnalysisStepExecutionEntity step3 = new()
        {
            Id = 3,
            Identifier = "exp_1_3",
            AnalysisIdentifier = "exp_1",
            StepName = "Simulation",
            StepOrder = 3,
            State = PipelineStepState.Faulted,
            InputData = [],
            OutputData = compressedOutput
        };

        _repositoryMock
            .Setup(r => r.GetAsync<AnalysisStepExecutionEntity, AnalysisStepExecutionFilter>(It.IsAny<AnalysisStepExecutionFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable([step1, step2, step3]));

        _fileStorageServiceMock
            .Setup(f => f.ExistsAsync(fileToPurge, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        StepExecutionManager manager = new(_loggerMock.Object, _fileStorageServiceMock.Object, _repositoryMock.Object, _timeProvider);

        // Act
        await manager.RollbackFromStepAsync("exp_1", 3);

        // Assert
        _fileStorageServiceMock.Verify(f => f.DeleteAsync(fileToPurge, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync<AnalysisStepExecutionEntity>(3, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync<AnalysisStepExecutionEntity>(1, It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.DeleteAsync<AnalysisStepExecutionEntity>(2, It.IsAny<CancellationToken>()), Times.Never);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
}
