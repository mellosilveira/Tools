using MelloSilveiraTools.Core.Models;

namespace MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.DataContract;

/// <summary>
/// Response content for async operations.
/// </summary>
public record AsyncOperationResponse<TResponseData> : Result
    where TResponseData : class
{
    /// <summary>
    /// Data content of response.
    /// </summary>
    public required IAsyncEnumerable<TResponseData> Data { get; set; }
}
