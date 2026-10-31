using MelloSilveiraTools.Core.Application.Commands;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Pipeline-agnostic request payload for retrying or restarting an analysis from a designated pipeline step.
/// </summary>
public record ResumeStepRequest : RequestBase
{
    /// <summary>
    /// Unique business identifier correlating the analysis.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// Logical name of the step to resume from.
    /// </summary>
    public required string FromStepName { get; init; }
}
