using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Serialization;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Services;

/// <summary>
/// Manages step execution checkpoints, persisting compressed payloads and orchestrating rollback operations.
/// </summary>
public sealed class StepExecutionManager(
    ILogger<StepExecutionManager> logger,
    IFileStorageService fileStorageService,
    IRepository repository,
    TimeProvider timeProvider)
    : IStepExecutionManager
{
    /// <inheritdoc/>
    public async Task RecordStepPendingAsync<TInput>(string analysisIdentifier, string stepName, int stepOrder, TInput input, CancellationToken cancellationToken = default)
    {
        string identifier = $"{analysisIdentifier}_{stepOrder}";
        byte[] inputData = StepPayloadCompressor.Compress(input);
        DateTimeOffset now = timeProvider.GetUtcNow();

        AnalysisStepExecutionEntity? existing = await repository.GetByUniqueColumnAsync<AnalysisStepExecutionEntity>(identifier, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            AnalysisStepExecutionEntity updated = existing with
            {
                StepName = stepName,
                State = PipelineStepState.Pending,
                InputData = inputData,
                OutputData = null,
                ErrorMessage = null,
                StartedAt = now,
                CompletedAt = null
            };

            await repository.TryUpdateAsync(updated, cancellationToken).ConfigureAwait(false);
            return;
        }

        AnalysisStepExecutionEntity entity = new()
        {
            Identifier = identifier,
            AnalysisIdentifier = analysisIdentifier,
            StepName = stepName,
            StepOrder = stepOrder,
            State = PipelineStepState.Pending,
            InputData = inputData,
            OutputData = null,
            ProcessedItems = 0,
            ErrorMessage = null,
            StartedAt = now,
            CompletedAt = null
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (!insertResult.Success && !insertResult.IsConflict)
        {
            logger.LogWarning("Failed to persist pending step execution checkpoint. Step: {StepName}, Identifier: {Identifier}. Errors: {Errors}", stepName, identifier, string.Join("; ", insertResult.Messages));
        }
    }

    /// <inheritdoc/>
    public async Task RecordStepStartedAsync<TInput>(string analysisIdentifier, string stepName, int stepOrder, TInput input, CancellationToken cancellationToken = default)
    {
        string identifier = $"{analysisIdentifier}_{stepOrder}";
        byte[] inputData = StepPayloadCompressor.Compress(input);
        DateTimeOffset now = timeProvider.GetUtcNow();

        AnalysisStepExecutionEntity? existing = await repository.GetByUniqueColumnAsync<AnalysisStepExecutionEntity>(identifier, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            AnalysisStepExecutionEntity updated = existing with
            {
                StepName = stepName,
                State = PipelineStepState.Running,
                InputData = inputData,
                OutputData = null,
                ErrorMessage = null,
                StartedAt = now,
                CompletedAt = null
            };

            await repository.TryUpdateAsync(updated, cancellationToken).ConfigureAwait(false);
            return;
        }

        AnalysisStepExecutionEntity entity = new()
        {
            Identifier = identifier,
            AnalysisIdentifier = analysisIdentifier,
            StepName = stepName,
            StepOrder = stepOrder,
            State = PipelineStepState.Running,
            InputData = inputData,
            OutputData = null,
            ProcessedItems = 0,
            ErrorMessage = null,
            StartedAt = now,
            CompletedAt = null
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (!insertResult.Success && !insertResult.IsConflict)
        {
            logger.LogWarning("Failed to persist started step execution checkpoint. Step: {StepName}, Identifier: {Identifier}. Errors: {Errors}", stepName, identifier, string.Join("; ", insertResult.Messages));
        }
    }

    /// <inheritdoc/>
    public async Task RecordStepCompletedAsync<TOutput>(string analysisIdentifier, string stepName, int stepOrder, TOutput output, int processedItems, CancellationToken cancellationToken = default)
    {
        string identifier = $"{analysisIdentifier}_{stepOrder}";
        byte[] outputData = StepPayloadCompressor.Compress(output);
        DateTimeOffset now = timeProvider.GetUtcNow();

        AnalysisStepExecutionEntity? existing = await repository.GetByUniqueColumnAsync<AnalysisStepExecutionEntity>(identifier, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            AnalysisStepExecutionEntity updated = existing with
            {
                State = PipelineStepState.Completed,
                OutputData = outputData,
                ProcessedItems = processedItems,
                CompletedAt = now
            };

            await repository.TryUpdateAsync(updated, cancellationToken).ConfigureAwait(false);
            return;
        }

        AnalysisStepExecutionEntity entity = new()
        {
            Identifier = identifier,
            AnalysisIdentifier = analysisIdentifier,
            StepName = stepName,
            StepOrder = stepOrder,
            State = PipelineStepState.Completed,
            InputData = [],
            OutputData = outputData,
            ProcessedItems = processedItems,
            ErrorMessage = null,
            StartedAt = now,
            CompletedAt = now
        };

        await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RecordStepFaultedAsync(string analysisIdentifier, string stepName, int stepOrder, string errorMessage, CancellationToken cancellationToken = default)
    {
        string identifier = $"{analysisIdentifier}_{stepOrder}";
        DateTimeOffset now = timeProvider.GetUtcNow();

        AnalysisStepExecutionEntity? existing = await repository.GetByUniqueColumnAsync<AnalysisStepExecutionEntity>(identifier, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            AnalysisStepExecutionEntity updated = existing with
            {
                State = PipelineStepState.Faulted,
                ErrorMessage = errorMessage,
                CompletedAt = now
            };

            await repository.TryUpdateAsync(updated, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task RecordCurrentRunningStepFaultedAsync(string analysisIdentifier, string errorMessage, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AnalysisStepExecutionEntity> steps = await GetStepExecutionsAsync(analysisIdentifier, cancellationToken).ConfigureAwait(false);
        AnalysisStepExecutionEntity? activeStep = steps.LastOrDefault(s => s.State == PipelineStepState.Running)
            ?? steps.LastOrDefault(s => s.State == PipelineStepState.Pending)
            ?? steps.LastOrDefault();

        if (activeStep is not null)
        {
            await RecordStepFaultedAsync(analysisIdentifier, activeStep.StepName, activeStep.StepOrder, errorMessage, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AnalysisStepExecutionEntity>> GetStepExecutionsAsync(string analysisIdentifier, CancellationToken cancellationToken = default)
    {
        AnalysisStepExecutionFilter filter = new()
        {
            AnalysisIdentifier = analysisIdentifier
        };

        List<AnalysisStepExecutionEntity> list = [];
        await foreach (AnalysisStepExecutionEntity entity in repository.GetAsync<AnalysisStepExecutionEntity, AnalysisStepExecutionFilter>(filter, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            list.Add(entity);
        }

        return [.. list.OrderBy(s => s.StepOrder)];
    }

    /// <inheritdoc/>
    public async Task RollbackFromStepAsync(string analysisIdentifier, int fromStepOrder, CancellationToken cancellationToken = default)
    {
        AnalysisStepExecutionFilter filter = new()
        {
            AnalysisIdentifier = analysisIdentifier,
            MinStepOrder = fromStepOrder
        };

        List<AnalysisStepExecutionEntity> stepsToRollback = [];
        await foreach (AnalysisStepExecutionEntity entity in repository.GetAsync<AnalysisStepExecutionEntity, AnalysisStepExecutionFilter>(filter, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            stepsToRollback.Add(entity);
        }

        foreach (AnalysisStepExecutionEntity step in stepsToRollback.Where(s => s.StepOrder >= fromStepOrder).OrderByDescending(s => s.StepOrder))
        {
            if (step.OutputData is not null && step.OutputData.Length > 0)
            {
                await PurgeOutputStorageFilesAsync(step.OutputData, cancellationToken).ConfigureAwait(false);
            }

            await repository.DeleteAsync<AnalysisStepExecutionEntity>(step.Id, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Rolled back step execution checkpoint: {StepName} (Order: {StepOrder}) for analysis '{AnalysisIdentifier}'.", step.StepName, step.StepOrder, analysisIdentifier);
        }
    }

    private async Task PurgeOutputStorageFilesAsync(byte[] outputData, CancellationToken cancellationToken)
    {
        try
        {
            using JsonDocument? doc = StepPayloadCompressor.DecompressToDocument(outputData);
            if (doc is null || doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                if (property.Name.EndsWith("Location", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                {
                    string location = property.Value.GetString()!;
                    bool exists = await fileStorageService.ExistsAsync(location, cancellationToken).ConfigureAwait(false);
                    if (exists)
                    {
                        await fileStorageService.DeleteAsync(location, cancellationToken).ConfigureAwait(false);
                        logger.LogInformation("Purged storage file during step rollback: '{Location}'.", location);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to purge storage files from output payload during rollback.");
        }
    }
}
