using MelloSilveiraTools.Application.Operations;

namespace MelloSilveiraTools.Infrastructure.Services.ApiServiceAgent.DataContract;

/// <summary>
/// Response content for async operations.
/// </summary>
public record AsyncOperationResponse<TResponseData> : OperationResponse
    where TResponseData : class
{
    /// <summary>
    /// Data content of response.
    /// </summary>
    public IAsyncEnumerable<TResponseData> Data { get; set; }
}
