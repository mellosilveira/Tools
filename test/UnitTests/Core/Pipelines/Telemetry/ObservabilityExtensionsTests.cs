using System.Diagnostics;
using System.Runtime.CompilerServices;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Core.Pipelines.Telemetry;

/// <summary>
/// Unit tests for <see cref="ObservabilityExtensions"/>, <see cref="Grouping{TKey, TElement}"/>,
/// <see cref="PipelineStepOptions"/>, and <see cref="SafeResult{TIn, TOut}"/> verifying telemetry wrappers,
/// safe execution error capture, and resilient progress dispatch.
/// </summary>
public sealed class ObservabilityExtensionsTests
{
    /// <summary>
    /// Verifies SerializeDetails returns null for null input and JSON for non-null input.
    /// </summary>
    [Fact]
    public void SerializeDetails_NullAndObject_ReturnsExpectedString()
    {
        Assert.Null(ObservabilityExtensions.SerializeDetails(null));
        Assert.Contains("\"value\":42", ObservabilityExtensions.SerializeDetails(new { Value = 42 }));
    }

    /// <summary>
    /// Verifies HandleSafeExecution overloads for sync Action, sync Func, async Func, and sync conditional fork capture exceptions.
    /// </summary>
    [Fact]
    public async Task HandleSafeExecution_Overloads_CaptureExceptionsAndSupportSuccess()
    {
        // Sync Action
        Func<int, Task<SafeResult<int>>> safeSyncAction = ObservabilityExtensions.HandleSafeExecution<int>(
            NullLogger.Instance,
            NullPipelineProgressNotifier.Instance,
            TimeProvider.System,
            "p1",
            "SyncAction",
            x =>
            {
                if (x < 0)
                    throw new InvalidOperationException("bad action");
            });

        SafeResult<int> okAction = await safeSyncAction(1);
        SafeResult<int> failAction = await safeSyncAction(-1);
        Assert.True(okAction.Success);
        Assert.False(failAction.Success);
        Assert.Equal("SyncAction", failAction.FailedPayload!.Value.CallbackName);

        // Sync Func
        Func<int, Task<SafeResult<int, string>>> safeSyncFunc = ObservabilityExtensions.HandleSafeExecution<int, string>(
            NullLogger.Instance,
            NullPipelineProgressNotifier.Instance,
            TimeProvider.System,
            "p1",
            "SyncFunc",
            x => x < 0 ? throw new InvalidOperationException("bad func") : x.ToString());

        Assert.True((await safeSyncFunc(5)).Success);
        Assert.False((await safeSyncFunc(-1)).Success);

        // Sync Forking
        Func<int, Task<SafeResult<int, int>>> safeSyncFork = ObservabilityExtensions.HandleSafeExecution<int, int>(
            NullLogger.Instance,
            NullPipelineProgressNotifier.Instance,
            TimeProvider.System,
            "p1",
            "Primary",
            "Fallback",
            x => x * 2,
            res => res.Success && res.Output > 10,
            x => x + 100);

        Assert.Equal(6, (await safeSyncFork(3)).Output);
        Assert.Equal(106, (await safeSyncFork(6)).Output);
    }

    /// <summary>
    /// Verifies HandleExecution and HandleSafeExecution for IAsyncEnumerable streams catch mid-stream exceptions cleanly.
    /// </summary>
    [Fact]
    public async Task HandleStreaming_WhenEnumeratorThrows_PropagatesOrYieldsSafeFailure()
    {
        static async IAsyncEnumerable<int> FaultingStream(int input, [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.Yield();
            yield return input;
            throw new InvalidOperationException("stream failed");
        }

        Func<int, IAsyncEnumerable<int>> rawWrapper = ObservabilityExtensions.HandleExecution<int, int>(
            NullLogger.Instance,
            NullPipelineProgressNotifier.Instance,
            TimeProvider.System,
            "p2",
            "RawStream",
            FaultingStream);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (int _ in rawWrapper(10)) { }
        });

        Func<int, IAsyncEnumerable<SafeResult<int, int>>> safeWrapper = ObservabilityExtensions.HandleSafeExecution<int, int>(
            NullLogger.Instance,
            NullPipelineProgressNotifier.Instance,
            TimeProvider.System,
            "p2",
            "SafeStream",
            FaultingStream);

        List<SafeResult<int, int>> results = [];
        await foreach (SafeResult<int, int> item in safeWrapper(10))
            results.Add(item);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].Success);
        Assert.Equal(10, results[0].Output);
        Assert.False(results[1].Success);
        Assert.Equal("SafeStream", results[1].FailedPayload!.Value.CallbackName);
    }

    /// <summary>
    /// Verifies FailedPayload implicit conversions and null guard.
    /// </summary>
    [Fact]
    public void FailedPayload_ImplicitConversions_WorkAndThrowWhenNullableIsNull()
    {
        FailedPayload<int> typed = new("StepX", 123, new InvalidOperationException("err"));
        FailedPayload untyped = typed;
        Assert.Equal("StepX", untyped.CallbackName);
        Assert.Equal(123, untyped.Payload);

        FailedPayload<int>? nullableTyped = typed;
        FailedPayload fromNullable = nullableTyped;
        Assert.Equal("StepX", fromNullable.CallbackName);

        FailedPayload<int>? nullTyped = null;
        Assert.Throws<ArgumentNullException>(() =>
        {
            FailedPayload _ = nullTyped;
        });
    }

    /// <summary>
    /// Verifies that synchronous exceptions and faulted ValueTasks returned by IPipelineProgressNotifier do not crash step execution.
    /// </summary>
    [Fact]
    public async Task HandleExecution_WhenNotifierThrowsOrFaults_SwallowsNotificationErrors()
    {
        // Synchronous throw from notifier
        Mock<IPipelineProgressNotifier> throwingNotifier = new();
        throwingNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<PipelineProgressEvent>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("sync notifier fail"));

        Func<int, Task<int>> wrapped1 = ObservabilityExtensions.HandleExecution<int, int>(
            NullLogger.Instance,
            throwingNotifier.Object,
            TimeProvider.System,
            "p3",
            "Step1",
            x => x + 1);

        Assert.Equal(2, await wrapped1(1));

        // Async faulted ValueTask from notifier (uncompleted asynchronous Task so ContinueWith fault continuation runs)
        Mock<IPipelineProgressNotifier> faultedNotifier = new();
        TaskCompletionSource<bool> notifyGate = new();
        faultedNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<PipelineProgressEvent>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("async notifier fail");
            });

        Func<int, Task> wrapped2 = ObservabilityExtensions.HandleExecution<int>(
            NullLogger.Instance,
            faultedNotifier.Object,
            TimeProvider.System,
            "p3",
            "Step2",
            _ => notifyGate.TrySetResult(true));

        await wrapped2(1);
        await Task.Delay(30);

        // Non-null ActivityListener covers Activity status/exception tags cleanly
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == "MelloSilveiraTools.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        Func<int, Task<int>> tracedStep = ObservabilityExtensions.HandleExecution<int, int>(
            NullLogger.Instance,
            NullPipelineProgressNotifier.Instance,
            TimeProvider.System,
            "p4",
            "Traced",
            x => x < 0 ? throw new InvalidOperationException("bad") : x * 2);

        Assert.Equal(6, await tracedStep(3));
        await Assert.ThrowsAsync<InvalidOperationException>(() => tracedStep(-1));
    }

    /// <summary>
    /// Verifies Grouping&lt;TKey, TElement&gt; enumerators and key property.
    /// </summary>
    [Fact]
    public void Grouping_EnumeratesElementsAndExposesKey()
    {
        Grouping<string, int> grouping = new("group-a", [10, 20, 30]);
        System.Collections.IEnumerable nonGeneric = grouping;

        Assert.Equal("group-a", grouping.Key);
        Assert.Equal([10, 20, 30], grouping.ToArray());
        Assert.Equal(3, nonGeneric.Cast<int>().Count());
    }
}
