using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Commands;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Validators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Core.Pipelines.Commands;

/// <summary>
/// Unit tests for <see cref="ExecutePipeline{TRequest, TInput, TOutput}"/> verifying background execution,
/// progress channel pre-allocation, Accepted result creation with mapped identifier, and fault handling.
/// </summary>
public sealed class ExecutePipelineTests
{
    public sealed record TestRequest(string RunId);
    public sealed record TestInput : PipelineInputBase;

    private sealed class TestExecutePipeline(
        ILogger<ExecutePipeline<TestRequest, TestInput, string>> logger,
        IPipelineProgressNotifier progressNotifier,
        IPipeline<TestInput, string> pipeline,
        IValidator<TestRequest> validator)
        : ExecutePipeline<TestRequest, TestInput, string>(logger, progressNotifier, pipeline, validator)
    {
        protected override TestInput MapToInput(TestRequest request) => new() { Identifier = request.RunId };
    }

    /// <summary>
    /// Verifies that ExecuteAsync pre-allocates the progress channel, returns Accepted with the mapped identifier, and runs ProcessAsync in the background.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ReturnsAcceptedAndProcessesPipelineInBackground()
    {
        // Arrange
        TaskCompletionSource<bool> processedTcs = new();
        Mock<IPipelineProgressNotifier> notifierMock = new();
        Mock<IPipeline<TestInput, string>> pipelineMock = new();
        pipelineMock
            .Setup(p => p.ProcessAsync(It.IsAny<TestInput>()))
            .ReturnsAsync(() =>
            {
                processedTcs.TrySetResult(true);
                return Result.CreateSuccessOk("done");
            });

        Mock<IValidator<TestRequest>> validatorMock = new();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.CreateSuccessOk());

        TestExecutePipeline command = new(NullLogger<ExecutePipeline<TestRequest, TestInput, string>>.Instance, notifierMock.Object, pipelineMock.Object, validatorMock.Object);

        // Act
        Result<string> result = await command.ExecuteAsync(new TestRequest("run-42"));
        await processedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.Accepted, result.StatusCode);
        Assert.Equal("run-42", result.Data);
        notifierMock.Verify(n => n.EnsureChannel("run-42"), Times.Once);
        pipelineMock.Verify(p => p.ProcessAsync(It.Is<TestInput>(i => i.Identifier == "run-42")), Times.Once);
    }

    /// <summary>
    /// Verifies that background pipeline faults are caught, logged, and complete the progress channel without crashing the command response.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenBackgroundPipelineThrows_LogsErrorAndReturnsAccepted()
    {
        // Arrange
        TaskCompletionSource<bool> faultedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> completedChannelTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IPipelineProgressNotifier> notifierMock = new();
        notifierMock
            .Setup(n => n.TryComplete("run-fail", It.IsAny<Exception?>()))
            .Callback(() => completedChannelTcs.TrySetResult(true))
            .Returns(true);

        Mock<IPipeline<TestInput, string>> pipelineMock = new();
        pipelineMock
            .Setup(p => p.ProcessAsync(It.IsAny<TestInput>()))
            .ThrowsAsync(new InvalidOperationException("Pipeline boom"));

        Mock<ILogger<ExecutePipeline<TestRequest, TestInput, string>>> loggerMock = new();
        loggerMock
            .Setup(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => faultedTcs.TrySetResult(true));

        Mock<IValidator<TestRequest>> validatorMock = new();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.CreateSuccessOk());

        TestExecutePipeline command = new(loggerMock.Object, notifierMock.Object, pipelineMock.Object, validatorMock.Object);

        // Act
        Result<string> result = await command.ExecuteAsync(new TestRequest("run-fail"));
        await Task.WhenAll(faultedTcs.Task, completedChannelTcs.Task).WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.Accepted, result.StatusCode);
        Assert.Equal("run-fail", result.Data);
        notifierMock.Verify(n => n.EnsureChannel("run-fail"), Times.Once);
        notifierMock.Verify(n => n.TryComplete("run-fail", It.Is<Exception?>(ex => ex is InvalidOperationException)), Times.Once);
    }
}
