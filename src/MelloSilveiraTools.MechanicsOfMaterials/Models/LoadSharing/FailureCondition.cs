namespace MelloSilveiraTools.MechanicsOfMaterials.Models.LoadSharing;

/// <summary>
/// Represents the failure conditions during load sharing analysis.
/// </summary>
public record FailureCondition
{
    /// <summary>
    /// The same identifier used for specimen in load sharing analysis.
    /// </summary>
    public required string[] SpecimenIdentifiers { get; init; }

    /// <summary>
    /// Time when the specimen failure.
    /// Unit: s (seconds)
    /// </summary>
    public double Time { get; init; }
}