namespace MelloSilveiraTools.UseCases.Services.ApiServiceAgent.Settings
{
    /// <summary>
    /// Settings for integrations with an API.
    /// </summary>
    public abstract class ApiServiceAgentSettings
    {
        /// <summary>
        /// Base address for integration with an API.
        /// </summary>
        public string BaseAddress { get; set; }

        /// <summary>
        /// Default timeout in seconds for conection with API.
        /// </summary>
        public int DefaultTimeoutInSeconds { get; set; }

        /// <summary>
        /// Default number of retries.
        /// </summary>
        public int DefaultRetryCount { get; set; }

        /// <summary>
        /// Default interval in miliseconds for retries.
        /// </summary>
        public int DefaultRetryIntervalInMiliseconds { get; set; }
    }
}
