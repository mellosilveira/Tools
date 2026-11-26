using MelloSilveiraTools.Core.Models;

namespace MelloSilveiraTools.Core.Validators;

/// <summary>
/// Validates a business request or domain input prior to command execution.
/// Returns <see cref="ValueTask{TResult}"/> so purely synchronous validators complete without heap allocation
/// while I/O-backed validators (storage, database) execute asynchronously without blocking threads.
/// </summary>
/// <typeparam name="T">The input type to validate.</typeparam>
public interface IValidator<T>
{
    /// <summary>
    /// Validates <paramref name="value"/> and returns a <see cref="Result"/> describing success or validation failures.
    /// </summary>
    /// <param name="value">Input object to validate.</param>
    /// <param name="cancellationToken">Token used to cancel asynchronous validation I/O.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> resolving to a successful or error <see cref="Result"/>.</returns>
    ValueTask<Result> ValidateAsync(T value, CancellationToken cancellationToken = default);
}
