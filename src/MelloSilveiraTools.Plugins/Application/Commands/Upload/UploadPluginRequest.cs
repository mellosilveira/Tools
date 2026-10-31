using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.Plugins.Application.Commands.Upload;

/// <summary>
/// Request containing a plugin assembly file to be uploaded to storage.
/// </summary>
public record UploadPluginRequest
{
    /// <summary>
    /// The plugin assembly file (.dll) complying with naming pattern {name}.v{major}.{minor}.{patch}.dll.
    /// </summary>
    public required IFormFile File { get; init; }
}
