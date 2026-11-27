using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;

namespace MelloSilveiraTools.Plugins.Application.Commands.Get;

/// <summary>
/// Superuser diagnostic command that scans the main plugin storage partitions (<c>loaded</c>, <c>incoming</c>,
/// <c>pending</c>, <c>archive</c>, <c>quarantine</c>, <c>rejected</c>) and returns physical
/// <see cref="Core.Files.Models.StorageEntryMetadata"/>, static PE/architecture validation results,
/// and runtime type registration status for active partitions.
/// Access is restricted to superusers at the HTTP authorization boundary (<c>SuperuserOnly</c>).
/// </summary>
/// <param name="pluginService">Plugin orchestration service used to inspect storage partitions and runtime registration state.</param>
/// <param name="validator">Validator ensuring any supplied semantic version filter is well-formed.</param>
public class InspectPlugins(IPluginService pluginService, PluginValidator<InspectPluginsRequest> validator) : ListedCommandBase<InspectPluginsRequest, PluginInfo>(validator)
{
    /// <inheritdoc />
    protected override async Task<ListedResult<PluginInfo>> ExecuteCommandAsync(InspectPluginsRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Partition) && !PluginStoragePartitions.MainPartitions.Contains(request.Partition, StringComparer.OrdinalIgnoreCase))
        {
            string validPartitions = string.Join(", ", PluginStoragePartitions.MainPartitions);
            return Result.CreateBadRequest($"Invalid plugin partition '{request.Partition}'. Supported partitions: {validPartitions}.");
        }

        return await pluginService
            .InspectPluginsAsync(request.Name, PluginVersion.SafeParse(request.Version), request.Partition, cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}