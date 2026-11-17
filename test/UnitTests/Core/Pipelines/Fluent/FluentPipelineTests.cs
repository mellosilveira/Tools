using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Fluent;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Core.Pipelines.Fluent;

/// <summary>
/// Unit tests for <see cref="FluentPipelineBuilder{TInitialIn, TCurrentOut}"/> and <see cref="PipelineFactory.StartFluent{T}"/>
/// verifying sequential execution, type transformation, error wrapping, and cancellation.
/// </summary>
public sealed class FluentPipelineTests
{
    private readonly PipelineFactory _factory = new(NullPipelineProgressNotifier.Instance, TimeProvider.System);

    /// <summary>
    /// Verifies that StartFluent chains synchronous and asynchronous steps and returns the expected output.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithSyncAndAsyncSteps_TransformsPayloadCorrectly()
    {
        // Arrange
        await using IFluentPipeline<int, string> pipeline = _factory
            .StartFluent<int>()
            .AddStep("Multiply", x => x * 3)
            .AddStep("FormatAsync", async (x, ct) =>
            {
                await Task.Yield();
                return $"Value={x}";
            })
            .Build(NullLogger.Instance);

        // Act
        string result = await pipeline.ExecuteAsync(7);

        // Assert
        Assert.Equal("Value=21", result);
    }

    /// <summary>
    /// Verifies that passing null input throws ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        await using IFluentPipeline<string, int> pipeline = _factory
            .StartFluent<string>()
            .AddStep("Length", s => s.Length)
            .Build(NullLogger.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => pipeline.ExecuteAsync(null!));
    }

    /// <summary>
    /// Verifies that step failures are wrapped in PipelineExecutionException containing the failing step name.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenStepThrows_WrapsInPipelineExecutionException()
    {
        // Arrange
        await using IFluentPipeline<int, int> pipeline = _factory
            .StartFluent<int>()
            .AddStep("Step1", x => x + 1)
            .AddStep<int>("FaultyStep", _ => throw new InvalidOperationException("Boom"))
            .Build(NullLogger.Instance);

        // Act
        PipelineExecutionException ex = await Assert.ThrowsAsync<PipelineExecutionException>(() => pipeline.ExecuteAsync(5));

        // Assert
        Assert.Equal("FaultyStep", ex.StepName);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that cancellation before or during execution throws OperationCanceledException directly.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await using IFluentPipeline<int, int> pipeline = _factory
            .StartFluent<int>()
            .AddStep("Step1", x => x + 1)
            .Build(NullLogger.Instance);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pipeline.ExecuteAsync(1, cts.Token));
    }
}
