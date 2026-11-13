using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

namespace MelloSilveiraTools.Plugins.Application.Validators;

/// <summary>
/// Validates an incoming plugin assembly upload, enforcing naming conventions and managed .NET assembly requirements.
/// </summary>
public class UploadPluginValidator : IValidator<UploadPluginRequest>
{
    private static readonly Regex PluginFileRegex = new(@"^(?<name>.+)\.v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)\.dll$", RegexOptions.Compiled);

    /// <inheritdoc/>
    public Result Validate(UploadPluginRequest value)
    {
        if (value.File is null || value.File.Length == 0)
            return Result.CreateBadRequest("Plugin file is required and cannot be empty.");

        if (!value.File.FileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return Result.CreateBadRequest("Only .dll assembly files are permitted.");

        if (!PluginFileRegex.IsMatch(value.File.FileName))
            return Result.CreateBadRequest($"Plugin filename '{value.File.FileName}' does not conform to required pattern '{{name}}.v{{major}}.{{minor}}.{{patch}}.dll'.");

        try
        {
            using Stream stream = value.File.OpenReadStream();
            Stream seekableStream = stream;
            MemoryStream? memoryStream = null;
            if (!stream.CanSeek)
            {
                memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                seekableStream = memoryStream;
            }

            try
            {
                using PEReader peReader = new(seekableStream, PEStreamOptions.LeaveOpen);
                if (!peReader.HasMetadata)
                    return Result.CreateBadRequest("Uploaded file is not a valid managed .NET assembly.");
            }
            finally
            {
                memoryStream?.Dispose();
            }
        }
        catch (Exception ex)
        {
            return Result.CreateBadRequest($"Invalid PE binary format: {ex.Message}");
        }

        return Result.CreateSuccessOk();
    }
}
