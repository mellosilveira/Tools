using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;

namespace MelloSilveiraTools.Plugins.Application.Commands.Submissions.Get;

/// <summary>
/// Retrieves plugin submissions visible to the current authenticated caller.
/// Authentication is enforced at the HTTP authorization boundary (<c>[Authorize]</c> / <c>.RequireAuthorization()</c>),
/// and PostgreSQL Row-Level Security guarantees that regular users only see approved plugins and their own submissions,
/// while superusers see the complete moderation queue.
/// </summary>
/// <param name="repository">Repository querying <see cref="PluginSubmissionEntity"/>.</param>
public class GetPluginSubmissions(IRepository repository) : ListedCommandBase<GetPluginSubmissionsRequest, PluginInfo>()
{
    /// <inheritdoc/>
    protected override async Task<ListedResult<PluginInfo>> ExecuteCommandAsync(GetPluginSubmissionsRequest request, CancellationToken cancellationToken = default)
    {
        PluginSubmissionFilter filter = new()
        {
            PluginName = string.IsNullOrWhiteSpace(request.PluginName) ? null : request.PluginName.Trim(),
            Status = request.Status
        };

        return await repository
            .GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(filter, cancellationToken: cancellationToken)
            .Select(PluginInfo.FromSubmissionEntity)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
