using MelloSilveiraTools.WebApi.Application.Models;
using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.WebApi.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="IResultExtensions"/> that produce NDJSON results.
/// </summary>
public static class NdjsonResultExtensions
{
    /// <summary>
    /// Builds an <see cref="IResult"/> that streams the supplied asynchronous sequence as newline-delimited JSON.
    /// </summary>
    public static IResult Ndjson<T>(this IResultExtensions _, IAsyncEnumerable<T> data) => new NdjsonResult<T>(data);
}
