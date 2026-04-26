using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace MelloSilveiraTools.WebApi.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="IFormFile"/>.
/// </summary>
public static class FormFileExtensions
{
    /// <summary>
    /// Reads the entire <paramref name="formFile"/> content and returns it compressed using GZip.
    /// </summary>
    public static async Task<byte[]> ToCompressedContentAsync(this IFormFile formFile)
    {
        await using Stream stream = formFile.OpenReadStream();
        await using MemoryStream memoryStream = new();
        await using GZipStream gzipStream = new(memoryStream, CompressionMode.Compress, leaveOpen: true);

        await stream.CopyToAsync(gzipStream);
        return memoryStream.ToArray();
    }
}
