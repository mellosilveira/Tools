using MelloSilveiraTools.UseCases.Operations;

namespace MelloSilveiraTools.UseCases.Services.ApiServiceAgent.DataContract
{
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
}
