namespace MelloSilveiraTools.UseCases.Services.ApiServiceAgent.Settings
{
    /// <summary>
    /// General settings for an endpoint.
    /// </summary>
    public class EndpointSettings
    {
        /// <summary>
        /// Endpoint URI.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Timeout in miliseconds for endpoint.
        /// </summary>
        public int TimeoutInMiliseconds { get; set; }
    }
}
