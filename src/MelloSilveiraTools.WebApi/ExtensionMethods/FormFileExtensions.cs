using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace MelloSilveiraTools.WebApi.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="IFormFile"/>.
/// </summary>
public static class FormFileExtensions
{
    extension(IFormFile formFile)
    {
        /// <summary>
        /// Reads the entire <paramref name="formFile"/> content and returns it compressed using GZip.
        /// </summary>
        public async Task<byte[]> ToCompressedContentAsync()
        {
            await using Stream stream = formFile.OpenReadStream();
            await using MemoryStream memoryStream = new();
            await using GZipStream gzipStream = new(memoryStream, CompressionMode.Compress, leaveOpen: true);

            await stream.CopyToAsync(gzipStream);
            return memoryStream.ToArray();
        }
    }
}
