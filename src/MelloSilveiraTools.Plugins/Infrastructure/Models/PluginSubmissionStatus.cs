namespace MelloSilveiraTools.Plugins.Infrastructure.Models;

/// <summary>
/// Moderation lifecycle state for a community or system plugin submission.
/// Numeric values are persisted in PostgreSQL (<c>plugin_submissions.status</c>) and enforced by RLS policies.
/// </summary>
public enum PluginSubmissionStatus
{
    /// <summary>Submitted by an authenticated user to <c>pending/</c>; awaiting superuser review.</summary>
    Pending = 0,

    /// <summary>Approved by a superuser (or uploaded directly by a superuser); promoted to <c>incoming/</c> and loaded.</summary>
    Approved = 1,

    /// <summary>Rejected by a superuser during review and moved to <c>rejected/</c>.</summary>
    Rejected = 2,

    /// <summary>Previously approved plugin subsequently revoked by a superuser.</summary>
    Revoked = 3
}
