using MelloSilveiraTools.Plugins.Infrastructure.Models;

namespace MelloSilveiraTools.Plugins.Application.Commands.Submissions.Get;

/// <summary>
/// Query criteria for listing plugin submissions.
/// </summary>
public sealed record GetPluginSubmissionsRequest
{
    /// <summary>Optional plugin name filter.</summary>
    public string? PluginName { get; init; }

    /// <summary>Optional moderation status filter.</summary>
    public PluginSubmissionStatus? Status { get; init; }
}
