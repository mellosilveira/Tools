namespace MelloSilveiraTools.Plugins.Application.Commands.Submissions.Cancel;

/// <summary>
/// Request payload for canceling a pending plugin submission.
/// </summary>
public sealed record CancelPluginSubmissionRequest
{
    /// <summary>Identifier of the pending plugin submission to cancel.</summary>
    public required long Id { get; init; }
}
