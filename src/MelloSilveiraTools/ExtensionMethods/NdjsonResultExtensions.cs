using MelloSilveiraTools.Application.Models;
using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.ExtensionMethods;

public static class NdjsonResultExtensions
{
    public static IResult Ndjson<T>(this IResultExtensions _, IAsyncEnumerable<T> data) => new NdjsonResult<T>(data);
}