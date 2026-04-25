namespace MelloSilveiraTools.Application.Models;

/// <summary>
/// Provides constants used by the application layer.
/// </summary>
public class ApplicationConstants
{
    /// <summary>
    /// Content type for newline-delimited JSON streaming responses.
    /// </summary>
    public const string NdjsonContentType = "application/x-ndjson";

    /// <summary>
    /// Value used for the X-Content-Type-Options header to prevent MIME sniffing.
    /// </summary>
    public const string NoSniffHeaderValue = "nosniff";

    /// <summary>
    /// Name of the HTTP trailer used to report the final status of a stream.
    /// </summary>
    public const string StreamStatusTrailerName = "X-Stream-Status";

    /// <summary>
    /// Value written to the stream status trailer when streaming completes successfully.
    /// </summary>
    public const string StreamSuccessfullyStatus = "true";
}
