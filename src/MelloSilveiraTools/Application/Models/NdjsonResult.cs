using MelloSilveiraTools.ExtensionMethods;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace MelloSilveiraTools.Application.Models;

/// <summary>
/// An <see cref="IResult"/> implementation that streams a sequence of items as newline-delimited JSON to the HTTP response.
/// </summary>
/// <typeparam name="T">The type of each item written to the response stream.</typeparam>
public class NdjsonResult<T>(IAsyncEnumerable<T> data) : IResult
{
    /// <summary>
    /// Executes the result, writing each item of the source sequence as an NDJSON line to the response body.
    /// </summary>
    /// <param name="httpContext">The HTTP context to which the NDJSON payload will be written.</param>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        // The nosniff directive within the X-Content-Type-Options HTTP response header is a security measure designed to
        // prevent browsers from performing MIME type sniffing.
        // When the X-Content-Type-Options header is set to nosniff, it instructs the browser to:
        // - Strictly adhere to the declared Content-Type header: The browser will not attempt to guess or override the MIME
        //   type based on the content of the response.
        // - Block requests if MIME type mismatch:
        //   - If a resource is requested as a specific type (e.g., a script) but the declared Content-Type does not match
        //   a valid MIME type for that resource (e.g., not a JavaScript MIME type), the browser will block the request.
        httpContext.Response.Headers.Append(HeaderNames.XContentTypeOptions, ApplicationConstants.NoSniffHeaderValue);
        httpContext.Response.ContentType = ApplicationConstants.NdjsonContentType;

        try
        {
            await foreach (var item in data.WithCancellation(httpContext.RequestAborted))
            {
                await httpContext.Response.WriteLineAsNdJsonAsync(item, httpContext.RequestAborted);
            }
        }
        catch (Exception ex)
        {
            throw new NdjsonException(ex);
        }

        httpContext.Response.AppendTrailer(ApplicationConstants.StreamStatusTrailerName, ApplicationConstants.StreamSuccessfullyStatus);
    }
}
