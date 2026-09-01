namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Defines optional retry configurations with exponential backoff for transient failure recovery.
/// </summary>
public readonly record struct RetryOptions(int MaxAttempts = 3, int InitialDelayMs = 500, double BackoffFactor = 2.0);
