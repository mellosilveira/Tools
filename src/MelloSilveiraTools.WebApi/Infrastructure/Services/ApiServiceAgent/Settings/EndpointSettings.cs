namespace MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.Settings;

/// <summary>
/// General settings for an endpoint.
/// </summary>
public record EndpointSettings
{
    /// <summary>
    /// Endpoint URI.
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// Timeout in miliseconds for endpoint.
    /// </summary>
    public int? TimeoutInMiliseconds { get; init; }
}
