using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace MelloSilveiraTools.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="IFormFile"/>.
/// </summary>
public static class FormFileExtensions
{
    public static async Task<byte[]> ToCompressedContentAsync(this IFormFile formFile)
    {
        await using var stream = formFile.OpenReadStream();
        await using MemoryStream memoryStream = new();
        await using (GZipStream gzipStream = new(memoryStream, CompressionMode.Compress, leaveOpen: true))
        {
            await stream.CopyToAsync(gzipStream);
        }

        return memoryStream.ToArray();
    }
}
