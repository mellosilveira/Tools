namespace MelloSilveiraTools.Plugins.Application.Commands.Submissions.Review;

/// <summary>
/// Request payload for superuser approval or rejection of a pending plugin submission.
/// </summary>
public sealed record ReviewPluginSubmissionRequest
{
    /// <summary>Identifier of the target plugin submission.</summary>
    public required long Id { get; init; }

    /// <summary>Optional reviewer notes explaining the approval or rejection decision.</summary>
    public string? ReviewNotes { get; init; }
}
