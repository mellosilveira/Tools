using SoftTissue.Domain.Models;
using SoftTissue.SharedModules.ExtensionMethods;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.LoadSharing;

/// <summary>
/// Contains the result for load sharing analysis.
/// </summary>
public class LoadSharingResult : AnalysisResult
{
    /// <summary>
    /// Specimens load sharing's result.
    /// </summary>
    public SpecimenLoadSharingResult[] SpecimensLoadSharingResult { get; init; }

    /// <summary>
    /// Unit: m (meter).
    /// </summary>
    public double SystemDisplacement { get; init; }

    /// <summary>
    /// Unit: N (Newton).
    /// </summary>
    public double SystemForce { get; init; }

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is LoadSharingResult result && this == result;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(SystemDisplacement, SystemForce);

    /// <summary>
    /// Returns a <see cref="bool"/> that indicates whether two specified <see cref="LoadSharingResult"/> values are equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True, if left and right are equal. False, otherwise.</returns>
    public static bool operator ==(LoadSharingResult left, LoadSharingResult right)
    {
        if (!left.SystemDisplacement.EqualsWithTolerance(right.SystemDisplacement)
            || !left.SystemForce.EqualsWithTolerance(right.SystemForce))
            return false;

        var leftById = left.SpecimensLoadSharingResult.ToDictionary(st => st.Identifier);
        return right.SpecimensLoadSharingResult.All(st =>
            leftById.TryGetValue(st.Identifier, out var match) && st.Equals(match));
    }

    /// <summary>
    /// Returns a <see cref="bool"/> that indicates whether two specified <see cref="LoadSharingResult"/> values are not equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True, if left and right are not equal. False, otherwise.</returns>
    public static bool operator !=(LoadSharingResult left, LoadSharingResult right) => !(left == right);
}