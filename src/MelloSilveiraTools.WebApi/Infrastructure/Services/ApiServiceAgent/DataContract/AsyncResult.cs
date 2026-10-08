using MelloSilveiraTools.Core.Models;

namespace MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.DataContract;

/// <summary>
/// Response content for async operations.
/// </summary>
public record AsyncResult<TResultData> : ResultBase where TResultData : class
{
    /// <summary>
    /// Data content of response.
    /// </summary>
    public IAsyncEnumerable<TResultData>? Data { get; set; }

    public static implicit operator AsyncResult<TResultData>(Result response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}
