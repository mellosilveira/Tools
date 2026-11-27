namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;

/// <summary>
/// Represents an individual experimental data group (strain.csv and stress.csv pair) within a batch pipeline execution request.
/// </summary>
public record ExperimentalDataGroupRequest
{
    /// <summary>
    /// Optional predefined analysis identifier. If not provided, a unique GUID-based identifier is generated.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// Optional threshold time to ignore initial transient data for this specific group.
    /// </summary>
    public double? StartExperimentalTimeThreshold { get; init; }

    /// <summary>
    /// Storage location URI or path for the raw experimental strain time-history CSV.
    /// </summary>
    public required string StrainFileLocation { get; init; }

    /// <summary>
    /// Storage location URI or path for the raw experimental stress time-history CSV.
    /// </summary>
    public required string StressFileLocation { get; init; }
}
