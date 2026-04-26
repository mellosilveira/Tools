using MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;

namespace MelloSilveiraTools.MechanicsOfMaterials.GeometricProperties;

/// <summary>
/// Calculates the cross-sectional geometric properties (area, moment of inertia, etc.) of a
/// structural profile used in Mechanics of Materials analyses.
/// </summary>
/// <typeparam name="TProfile">The concrete <see cref="Profile"/> type whose geometric properties are evaluated.</typeparam>
public interface IGeometricPropertyCalculator<TProfile>
    where TProfile : Profile
{
    /// <summary>
    /// Calculates the cross-sectional area of the supplied profile. If the profile defines a
    /// wall thickness the hollow-section area is returned; otherwise the solid-section area is used.
    /// </summary>
    /// <param name="profile">The profile whose cross-sectional area is to be computed.</param>
    /// <returns>The cross-sectional area in mm² (millimeters squared).</returns>
    double CalculateArea(TProfile profile);

    /// <summary>
    /// Calculates the second moment of area (area moment of inertia) of the supplied profile
    /// about its neutral axis. Hollow-section inertia is returned when a wall thickness is defined.
    /// </summary>
    /// <param name="profile">The profile whose moment of inertia is to be computed.</param>
    /// <returns>The moment of inertia in mm^4 (millimeters to the fourth power).</returns>
    double CalculateMomentOfInertia(TProfile profile);
}