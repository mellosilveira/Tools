using MelloSilveiraTools.WebApi.Application.Operations;

namespace MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.DataContract;

/// <summary>
/// Response content for async operations.
/// </summary>
public record AsyncOperationResponse<TResponseData> : OperationResponse
    where TResponseData : class
{
    /// <summary>
    /// Data content of response.
    /// </summary>
    public required IAsyncEnumerable<TResponseData> Data { get; set; }
}
