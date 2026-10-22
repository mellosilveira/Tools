using MelloSilveiraTools.Core.Pipelines.Models;

namespace UnitTests.Core.Pipelines.Models;

/// <summary>
/// Unit tests for <see cref="PipelineProgressEvent"/> and <see cref="PipelineStepState"/>.
/// </summary>
public sealed class PipelineProgressEventTests
{
    /// <summary>
    /// Verifies that all properties are assigned correctly when initialized via object initializer.
    /// </summary>
    [Fact]
    public void ObjectInitializer_AssignsAllPropertiesCorrectly()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Act
        PipelineProgressEvent progressEvent = new()
        {
            StepName = "TestStep",
            Description = "Processing test batch",
            State = PipelineStepState.Running,
            Timestamp = now,
            UpstreamSteps = ["ParentStepA", "ParentStepB"],
            ReceivedItems = 10,
            ProcessedItems = 5,
            ProgressPercentage = 50.0,
            Details = "Batch 1 of 5"
        };

        // Assert
        Assert.Equal("TestStep", progressEvent.StepName);
        Assert.Equal("Processing test batch", progressEvent.Description);
        Assert.Equal(PipelineStepState.Running, progressEvent.State);
        Assert.Equal(["ParentStepA", "ParentStepB"], progressEvent.UpstreamSteps);
        Assert.Equal(10, progressEvent.ReceivedItems);
        Assert.Equal(5, progressEvent.ProcessedItems);
        Assert.Equal(50.0, progressEvent.ProgressPercentage);
        Assert.Equal("Batch 1 of 5", progressEvent.Details);
        Assert.Equal(now, progressEvent.Timestamp);
    }

    /// <summary>
    /// Verifies that optional properties default to expected values when omitted.
    /// </summary>
    [Fact]
    public void ObjectInitializer_SetsDefaultValuesForOptionalProperties()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Act
        PipelineProgressEvent progressEvent = new()
        {
            StepName = "StepA",
            Description = "Initial step",
            State = PipelineStepState.Started,
            Timestamp = now
        };

        // Assert
        Assert.Equal("StepA", progressEvent.StepName);
        Assert.Equal("Initial step", progressEvent.Description);
        Assert.Equal(PipelineStepState.Started, progressEvent.State);
        Assert.Empty(progressEvent.UpstreamSteps);
        Assert.Equal(0, progressEvent.ReceivedItems);
        Assert.Equal(0, progressEvent.ProcessedItems);
        Assert.Equal(0.0, progressEvent.ProgressPercentage);
        Assert.Null(progressEvent.Details);
        Assert.Equal(now, progressEvent.Timestamp);
    }

    /// <summary>
    /// Verifies value-based record equality for <see cref="PipelineProgressEvent"/> including sequence equality of <see cref="PipelineProgressEvent.UpstreamSteps"/>.
    /// </summary>
    [Fact]
    public void RecordEquality_EvaluatesIdenticalPropertiesAsEqual()
    {
        // Arrange
        DateTimeOffset timestamp = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        PipelineProgressEvent event1 = new()
        {
            StepName = "Step1",
            Description = "Desc",
            State = PipelineStepState.Completed,
            Timestamp = timestamp,
            UpstreamSteps = ["RootStep"],
            ReceivedItems = 4,
            ProcessedItems = 4,
            ProgressPercentage = 100.0,
            Details = "OK"
        };
        PipelineProgressEvent event2 = new()
        {
            StepName = "Step1",
            Description = "Desc",
            State = PipelineStepState.Completed,
            Timestamp = timestamp,
            UpstreamSteps = new List<string> { "RootStep" },
            ReceivedItems = 4,
            ProcessedItems = 4,
            ProgressPercentage = 100.0,
            Details = "OK"
        };
        PipelineProgressEvent event3 = event1 with { State = PipelineStepState.Faulted };

        // Assert
        Assert.Equal(event1, event2);
        Assert.Equal(event1.GetHashCode(), event2.GetHashCode());
        Assert.NotEqual(event1, event3);
    }

    /// <summary>
    /// Verifies that all expected lifecycle states exist in <see cref="PipelineStepState"/>.
    /// </summary>
    [Fact]
    public void PipelineStepState_DefinesExpectedLifecycleStates()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(PipelineStepState), PipelineStepState.Pending));
        Assert.True(Enum.IsDefined(typeof(PipelineStepState), PipelineStepState.Started));
        Assert.True(Enum.IsDefined(typeof(PipelineStepState), PipelineStepState.Running));
        Assert.True(Enum.IsDefined(typeof(PipelineStepState), PipelineStepState.Completed));
        Assert.True(Enum.IsDefined(typeof(PipelineStepState), PipelineStepState.Faulted));
    }
}
