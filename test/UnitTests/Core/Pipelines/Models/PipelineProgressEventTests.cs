using MelloSilveiraTools.Core.Pipelines.Models;

namespace UnitTests.Core.Pipelines.Models;

/// <summary>
/// Unit tests for <see cref="PipelineProgressEvent"/> and <see cref="PipelineStepState"/> verifying record contract,
/// default values, and lifecycle state enumeration.
/// </summary>
public sealed class PipelineProgressEventTests
{
    /// <summary>
    /// Verifies that all properties are assigned correctly when initialized via primary constructor.
    /// </summary>
    [Fact]
    public void Constructor_AssignsAllPropertiesCorrectly()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Act
        PipelineProgressEvent progressEvent = new(
            StepName: "TestStep",
            Description: "Processing test batch",
            State: PipelineStepState.Running,
            StepIndex: 1,
            TotalSteps: 5,
            Detail: "Batch 1 of 5",
            Timestamp: now);

        // Assert
        Assert.Equal("TestStep", progressEvent.StepName);
        Assert.Equal("Processing test batch", progressEvent.Description);
        Assert.Equal(PipelineStepState.Running, progressEvent.State);
        Assert.Equal(1, progressEvent.StepIndex);
        Assert.Equal(5, progressEvent.TotalSteps);
        Assert.Equal("Batch 1 of 5", progressEvent.Detail);
        Assert.Equal(now, progressEvent.Timestamp);
    }

    /// <summary>
    /// Verifies that optional parameters default to null or default timestamp when omitted.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultValuesForOptionalParameters()
    {
        // Act
        PipelineProgressEvent progressEvent = new(
            StepName: "StepA",
            Description: "Initial step",
            State: PipelineStepState.Started);

        // Assert
        Assert.Equal("StepA", progressEvent.StepName);
        Assert.Equal("Initial step", progressEvent.Description);
        Assert.Equal(PipelineStepState.Started, progressEvent.State);
        Assert.Null(progressEvent.StepIndex);
        Assert.Null(progressEvent.TotalSteps);
        Assert.Null(progressEvent.Detail);
        Assert.Equal(default, progressEvent.Timestamp);
    }

    /// <summary>
    /// Verifies value-based record equality for <see cref="PipelineProgressEvent"/>.
    /// </summary>
    [Fact]
    public void RecordEquality_EvaluatesIdenticalPropertiesAsEqual()
    {
        // Arrange
        DateTimeOffset timestamp = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        PipelineProgressEvent event1 = new("Step1", "Desc", PipelineStepState.Completed, 1, 3, "OK", timestamp);
        PipelineProgressEvent event2 = new("Step1", "Desc", PipelineStepState.Completed, 1, 3, "OK", timestamp);
        PipelineProgressEvent event3 = event1 with { State = PipelineStepState.Faulted };

        // Assert
        Assert.Equal(event1, event2);
        Assert.NotEqual(event1, event3);
    }

    /// <summary>
    /// Verifies that all expected lifecycle states exist in <see cref="PipelineStepState"/>.
    /// </summary>
    [Fact]
    public void PipelineStepState_DefinesExpectedLifecycleStates()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(PipelineStepState), PipelineStepState.Started));
        Assert.True(Enum.IsDefined(typeof(PipelineStepState), PipelineStepState.Running));
        Assert.True(Enum.IsDefined(typeof(PipelineStepState), PipelineStepState.Completed));
        Assert.True(Enum.IsDefined(typeof(PipelineStepState), PipelineStepState.Faulted));
    }
}
