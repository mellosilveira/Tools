using MelloSilveiraTools.Core.ResiliencePipelines;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="DefaultResiliencePipeline"/>.
/// </summary>
public static class DefaultResiliencePipelineExtensions
{
    extension(DefaultResiliencePipeline pipeline)
    {
        /// <summary>
        /// Executes <paramref name="callback"/> through <paramref name="pipeline"/>, passing
        /// <see langword="false"/> on the initial attempt and <see langword="true"/> on every subsequent retry attempt.
        /// </summary>
        /// <typeparam name="T">Type returned by <paramref name="callback"/>.</typeparam>
        /// <param name="callback">Delegate receiving <c>isRetry</c> (<see langword="false"/> on attempt 1, <see langword="true"/> on retries).</param>
        /// <param name="callerMemberName">Caller member name forwarded for retry logging.</param>
        /// <param name="callerFilePath">Caller file path forwarded for retry logging.</param>
        /// <returns>The asynchronous result returned by <paramref name="callback"/>.</returns>
        public ValueTask<T> ExecuteWithRetryFlagAsync<T>(Func<bool, ValueTask<T>> callback, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
        {
            int attempt = 0;
            return pipeline.ExecuteAsync(_ => callback(attempt++ > 0), callerMemberName, callerFilePath);
        }

        /// <summary>
        /// Executes <paramref name="callback"/> through <paramref name="pipeline"/>, passing
        /// <see langword="false"/> on the initial attempt and <see langword="true"/> on every subsequent retry attempt.
        /// </summary>
        /// <param name="callback">Delegate receiving <c>isRetry</c> (<see langword="false"/> on attempt 1, <see langword="true"/> on retries).</param>
        /// <param name="callerMemberName">Caller member name forwarded for retry logging.</param>
        /// <param name="callerFilePath">Caller file path forwarded for retry logging.</param>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous execution.</returns>
        public ValueTask ExecuteWithRetryFlagAsync(Func<bool, ValueTask> callback, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
        {
            int attempt = 0;
            return pipeline.ExecuteAsync(_ => callback(attempt++ > 0), callerMemberName, callerFilePath);
        }
    }
}
