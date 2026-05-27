using MelloSilveiraTools.Mathematics.Extensions;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.LoadSharing;

/// <summary>
/// Contains the output for load sharing analysis.
/// </summary>
public class LoadSharingOutput : TimebasedAnalysisOutput
{
    /// <summary>
    /// Specimens load sharing's output.
    /// </summary>
    public SpecimenLoadSharingOutput[] SpecimensLoadSharingOutput { get; init; } = [];

    /// <summary>
    /// Unit: m (meter).
    /// </summary>
    public double SystemDisplacement { get; init; }

    /// <summary>
    /// Unit: N (Newton).
    /// </summary>
    public double SystemForce { get; init; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is LoadSharingOutput output && this == output;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(SystemDisplacement, SystemForce);

    /// <summary>
    /// Returns a <see cref="bool"/> that indicates whether two specified <see cref="LoadSharingOutput"/> values are equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True, if left and right are equal. False, otherwise.</returns>
    public static bool operator ==(LoadSharingOutput left, LoadSharingOutput right)
    {
        if (!left.SystemDisplacement.EqualsWithTolerance(right.SystemDisplacement)
            || !left.SystemForce.EqualsWithTolerance(right.SystemForce))
            return false;

        var leftById = left.SpecimensLoadSharingOutput.ToDictionary(st => st.Identifier);
        return right.SpecimensLoadSharingOutput.All(st =>
            leftById.TryGetValue(st.Identifier, out var match) && st.Equals(match));
    }

    /// <summary>
    /// Returns a <see cref="bool"/> that indicates whether two specified <see cref="LoadSharingOutput"/> values are not equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True, if left and right are not equal. False, otherwise.</returns>
    public static bool operator !=(LoadSharingOutput left, LoadSharingOutput right) => !(left == right);
}