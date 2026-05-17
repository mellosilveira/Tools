using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Plugins.Application.Operations;
using MelloSilveiraTools.Plugins.Infrastructure.Models;

namespace MelloSilveiraTools.Plugins.Application.Validators;

public class PluginValidator : PluginValidator<PluginsRequest>;

public class PluginValidator<TRequest> : IValidator<TRequest> where TRequest : PluginsRequest
{
    public Result Validate(TRequest value) => string.IsNullOrWhiteSpace(value.Version) || PluginVersion.TryParse(value.Version, out _)
        ? Result.CreateSuccessOk()
        : Result.CreateBadRequest("Invalid plugin version.");
}
