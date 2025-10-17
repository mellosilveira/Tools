namespace MelloSilveiraTools.Infrastructure.Services.ApiServiceAgent;

/// <summary>
/// Contains the base implementations for integrations with an API.
/// </summary>
public interface IApiServiceAgent : IDisposable
{
    /// <summary>
    /// Name of service agent.
    /// </summary>
    string ServiceName { get; }
}