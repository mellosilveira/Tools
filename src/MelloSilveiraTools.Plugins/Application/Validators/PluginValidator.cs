using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Plugins.Application.Commands;
using MelloSilveiraTools.Plugins.Infrastructure.Models;

namespace MelloSilveiraTools.Plugins.Application.Validators;

/// <summary>
/// Validates a <see cref="PluginsRequest"/>, verifying that any specified plugin version adheres to the expected format.
/// Registered as Singleton in DI. Thread-safe.
/// </summary>
public class PluginValidator : PluginValidator<PluginsRequest>;

/// <summary>
/// Validates a plugin request of type <typeparamref name="TRequest"/>, verifying that any specified plugin version adheres to the expected format.
/// Registered as Singleton in DI. Thread-safe.
/// </summary>
/// <typeparam name="TRequest">The type of plugin request to validate, derived from <see cref="PluginsRequest"/>.</typeparam>
public class PluginValidator<TRequest> : IValidator<TRequest> where TRequest : PluginsRequest
{
    /// <summary>
    /// Validates the specified plugin request, ensuring that the version string conforms to <see cref="PluginVersion"/> semantics if present.
    /// </summary>
    /// <param name="value">The plugin request instance to validate.</param>
    /// <returns>A successful <see cref="Result"/> when the version is omitted, empty, or valid; otherwise, a 400 Bad Request <see cref="Result"/>.</returns>
    public Result Validate(TRequest value) => string.IsNullOrWhiteSpace(value.Version) || PluginVersion.TryParse(value.Version, out _)
        ? Result.CreateSuccessOk()
        : Result.CreateBadRequest("Invalid plugin version.");
}
