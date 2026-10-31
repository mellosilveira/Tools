//using MelloSilveiraTools.Core.Pipelines;
//using MelloSilveiraTools.Core.Pipelines.Fluent;
//using MelloSilveiraTools.Core.Pipelines.Models;
//using Microsoft.Extensions.Logging.Abstractions;

//namespace UnitTests.Core.Pipelines.Fluent;

///// <summary>
///// Unit tests for <see cref="PipelineFactory.StartFluent{T}"/> and <see cref="IFluentPipeline{TInitialIn, TFinalOut}"/>
///// verifying synchronous and asynchronous step chaining, type transitions, cancellation handling, fault localization, and disposal.
///// </summary>
//public sealed class FluentPipelineTests
//{
//    /// <summary>
//    /// Verifies that sequential synchronous steps execute in order and transform input data across types.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WithSyncSteps_TransformsDataAcrossTypesAsync()
//    {
//        // Arrange
//        IFluentPipeline<int, string> pipeline = PipelineFactory.StartFluent<int>()
//            .AddStep("DoubleStep", x => x * 2)
//            .AddStep("ToStringStep", x => $"Value:{x}")
//            .Build(NullLogger.Instance);

//        // Act
//        string result = await pipeline.ExecuteAsync(21);

//        // Assert
//        Assert.Equal("Value:42", result);

//        await pipeline.DisposeAsync();
//    }

//    /// <summary>
//    /// Verifies that asynchronous steps correctly execute and propagate outputs.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WithAsyncSteps_AwaitsAndPropagatesOutputAsync()
//    {
//        // Arrange
//        IFluentPipeline<string, int> pipeline = PipelineFactory.StartFluent<string>()
//            .AddStep("TrimAsync", async (input, _) =>
//            {
//                await Task.Yield();
//                return input.Trim();
//            })
//            .AddStep("ParseAsync", async (input, _) =>
//            {
//                await Task.Yield();
//                return int.Parse(input);
//            })
//            .Build(NullLogger.Instance);

//        // Act
//        int result = await pipeline.ExecuteAsync("   123   ");

//        // Assert
//        Assert.Equal(123, result);

//        await pipeline.DisposeAsync();
//    }

//    /// <summary>
//    /// Verifies that passing a pre-canceled token halts execution before running steps.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WithCancelledToken_ThrowsOperationCanceledExceptionAsync()
//    {
//        // Arrange
//        using CancellationTokenSource cts = new();
//        cts.Cancel();

//        IFluentPipeline<int, int> pipeline = PipelineFactory.StartFluent<int>()
//            .AddStep("AddOne", x => x + 1)
//            .Build(NullLogger.Instance);

//        // Act & Assert
//        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
//        {
//            await pipeline.ExecuteAsync(10, cts.Token);
//        });

//        await pipeline.DisposeAsync();
//    }

//    /// <summary>
//    /// Verifies that an unhandled exception within a step is wrapped in a <see cref="PipelineExecutionException"/>
//    /// that retains the step name and inner exception.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WhenStepFails_WrapsInPipelineExecutionExceptionAsync()
//    {
//        // Arrange
//        IFluentPipeline<string, int> pipeline = PipelineFactory.StartFluent<string>()
//            .AddStep<int>("FaultingStep", _ => throw new InvalidOperationException("Boom in step"))
//            .Build(NullLogger.Instance);

//        // Act & Assert
//        PipelineExecutionException exception = await Assert.ThrowsAsync<PipelineExecutionException>(async () =>
//        {
//            await pipeline.ExecuteAsync("test");
//        });

//        Assert.Equal("FaultingStep", exception.StepName);
//        Assert.NotNull(exception.InnerException);
//        Assert.IsType<InvalidOperationException>(exception.InnerException);

//        await pipeline.DisposeAsync();
//    }

//    /// <summary>
//    /// Verifies that passing null as input throws <see cref="ArgumentNullException"/>.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WithNullInput_ThrowsArgumentNullExceptionAsync()
//    {
//        // Arrange
//        IFluentPipeline<string, string> pipeline = PipelineFactory.StartFluent<string>()
//            .AddStep("Echo", x => x)
//            .Build(NullLogger.Instance);

//        // Act & Assert
//        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
//        {
//            await pipeline.ExecuteAsync(null!);
//        });

//        await pipeline.DisposeAsync();
//    }
//}
