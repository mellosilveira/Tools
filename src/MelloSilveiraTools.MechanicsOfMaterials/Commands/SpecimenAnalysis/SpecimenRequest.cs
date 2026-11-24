namespace MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;

/// <summary>
/// Represents the reference geometry and initial kinematic boundary conditions for an isolated specimen
/// prior to executing a numerical loading protocol.
/// </summary>
public sealed record SpecimenRequest
{
    /// <summary>
    /// Indicates whether the numerical solver should bypass infinitesimal strain assumptions and employ
    /// finite strain continuum mechanics.
    /// </summary>
    public bool? ConsiderLargeDisplacement { get; init; }

    /// <summary>
    /// Specifies if the integration scheme must dynamically update spatial orientation and account for
    /// rotational kinematics of the load vector during deformation.
    /// </summary>
    public bool? ConsiderAngleVariation { get; init; }

    /// <summary>
    /// The reference undeformed cross-sectional area (A₀) of the specimen in mm².
    /// </summary>
    public double AreaInSquareMillimeters { get; init; }

    /// <summary>
    /// The initial spatial orientation angle of the specimen formatted as a parseable 3D vector string (e.g. <c>"0;0;0"</c>).
    /// </summary>
    public required string InitialAngleAsString { get; init; }

    /// <summary>
    /// The baseline pre-conditioning strain (ε₀) applied prior to the primary analytical step.
    /// </summary>
    public double PreLoadStrain { get; init; }

    /// <summary>
    /// The reference undeformed length (L₀) of the specimen in millimeters (mm).
    /// </summary>
    public double InitialLengthInMillimeters { get; init; }
}
