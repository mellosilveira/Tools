namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Defines optional retry configurations with exponential backoff for transient failure recovery.
/// </summary>
/// <remarks>
/// Retry logic is strictly reserved for asynchronous pipeline execution. 
/// Synchronous functions do not support retries because pausing execution would require blocking threads 
/// (e.g., <see cref="Thread.Sleep(TimeSpan)"/>), which quickly leads to ThreadPool starvation in a TPL 
/// Dataflow architecture. 
/// Additionally, synchronous data mapping operations are typically CPU-bound and fail deterministically, 
/// making them unsuitable for transient fault backoff strategies.
/// </remarks>
public readonly record struct RetryOptions(int MaxAttempts = 3, int InitialDelayMs = 500, double BackoffFactor = 1);
