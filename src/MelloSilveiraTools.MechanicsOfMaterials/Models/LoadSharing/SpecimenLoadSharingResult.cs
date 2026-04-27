using MelloSilveiraTools.Mathematics.Models;
using SoftTissue.SharedModules.ExtensionMethods;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.LoadSharing;

/// <summary>
/// Contains the result for an unique specimen on load sharing analysis.
/// </summary>
public class SpecimenLoadSharingResult
{
    private bool _loadSharingSet;

    /// <summary>
    /// Unique identifier for specimen.
    /// </summary>
    public string Identifier { get; init; }

    /// <summary>
    /// Unit: rad (radians).
    /// </summary>
    public Vector3D Angle { get; init; }

    /// <summary>
    /// Dimensionless.
    /// </summary>
    public double Strain { get; init; }

    /// <summary>
    /// Unit: m (meter).
    /// </summary>
    public double Displacement { get; init; }

    /// <summary>
    /// Unit: N (Newton).
    /// </summary>
    public double Force { get; init; }

    /// <summary>
    /// Unit: N (Newton).
    /// </summary>
    public double ForceOnSystemAxis { get; init; }

    /// <summary>
    /// Unit: MPa (Mega-Pascal).
    /// </summary>
    public double Stress { get; init; }

    /// <summary>
    /// Unit: dimensionless.
    /// </summary>
    public double LoadSharing { get; private set; }

    /// <summary>
    /// Sets a value for <see cref="LoadSharing"/>.
    /// </summary>
    /// <param name="loadSharing"></param>
    /// <exception cref="ArgumentException">When <see cref="LoadSharing"/> was already set.</exception>
    public void SetLoadSharing(double loadSharing)
    {
        if (!_loadSharingSet)
        {
            LoadSharing = loadSharing;
            _loadSharingSet = true;
        }
        else
        {
            throw new ArgumentException("It is not possible to attribute a value to property.", nameof(LoadSharing));
        }
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is SpecimenLoadSharingResult result)
        {
            return result.Angle == Angle
                   && Displacement.EqualsWithTolerance(result.Displacement)
                   && Strain.EqualsWithTolerance(result.Strain)
                   && Force.EqualsWithTolerance(result.Force)
                   && Stress.EqualsWithTolerance(result.Stress)
                   && LoadSharing.EqualsWithTolerance(result.LoadSharing);
        }

        return false;
    }

    /// <summary>
    /// This method was not implemented since it is not necessary.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override int GetHashCode() => HashCode.Combine(Identifier, Strain, Displacement, Force, Stress, LoadSharing);
}