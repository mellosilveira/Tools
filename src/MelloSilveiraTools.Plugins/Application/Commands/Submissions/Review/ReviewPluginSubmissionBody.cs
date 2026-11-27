namespace MelloSilveiraTools.Plugins.Application.Commands.Submissions.Review;

/// <summary>
/// Body payload accepted by approve/reject endpoints when the submission ID is supplied in the route.
/// </summary>
public sealed record ReviewPluginSubmissionBody
{
    /// <summary>Optional reviewer notes explaining the approval or rejection decision.</summary>
    public string? ReviewNotes { get; init; }
}
