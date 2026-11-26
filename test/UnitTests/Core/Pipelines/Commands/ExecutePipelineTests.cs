using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Commands;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Validators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Core.Pipelines.Commands;

/// <summary>
/// Unit tests for <see cref="ExecutePipeline{TRequest, TInput, TOutput}"/> verifying background execution,
/// Accepted result creation with mapped identifier, and fault logging.
/// </summary>
public sealed class ExecutePipelineTests
{
    public sealed record TestRequest(string RunId);
    public sealed record TestInput : PipelineInputBase;

    private sealed class TestExecutePipeline(
        ILogger<ExecutePipeline<TestRequest, TestInput, string>> logger,
        IPipeline<TestInput, string> pipeline,
        IValidator<TestRequest> validator)
        : ExecutePipeline<TestRequest, TestInput, string>(logger, pipeline, validator)
    {
        protected override TestInput MapToInput(TestRequest request) => new() { Identifier = request.RunId };
    }

    /// <summary>
    /// Verifies that ExecuteAsync returns Accepted with the mapped identifier and runs ProcessAsync in the background.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ReturnsAcceptedAndProcessesPipelineInBackground()
    {
        // Arrange
        TaskCompletionSource<bool> processedTcs = new();
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

        TestExecutePipeline command = new(NullLogger<ExecutePipeline<TestRequest, TestInput, string>>.Instance, pipelineMock.Object, validatorMock.Object);

        // Act
        Result<string> result = await command.ExecuteAsync(new TestRequest("run-42"));
        await processedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.Accepted, result.StatusCode);
        Assert.Equal("run-42", result.Data);
        pipelineMock.Verify(p => p.ProcessAsync(It.Is<TestInput>(i => i.Identifier == "run-42")), Times.Once);
    }

    /// <summary>
    /// Verifies that background pipeline faults are caught and logged without crashing the command response.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenBackgroundPipelineThrows_LogsErrorAndReturnsAccepted()
    {
        // Arrange
        TaskCompletionSource<bool> faultedTcs = new();
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

        TestExecutePipeline command = new(loggerMock.Object, pipelineMock.Object, validatorMock.Object);

        // Act
        Result<string> result = await command.ExecuteAsync(new TestRequest("run-fail"));
        await faultedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.Accepted, result.StatusCode);
        Assert.Equal("run-fail", result.Data);
    }
}
