using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;

/// <summary>
/// Provides extension methods for generating discrete values from a RangeParameters definition.
/// </summary>
public static class RangeParametersExtensions
{
    /// <summary>
    /// Evaluates and yields all discrete points within the specified range.
    /// </summary>
    /// <param name="range">The range parameters defining bounds and progression logic.</param>
    /// <returns>A stream of generated values.</returns>
    public static IEnumerable<double> GenerateValues(this RangeParameters? range)
    {
        if (range is null)
            yield break;

        if (range.FinalPoint is null || (range.Step is null && range.MultiplicativeFactor is null))
        {
            yield return range.InitialPoint;
            yield break;
        }

        double currentValue = range.InitialPoint;

        if (range.Step.HasValue)
        {
            while (currentValue <= range.FinalPoint.Value)
            {
                yield return currentValue;
                currentValue += range.Step.Value;
            }
        }
        else if (range.MultiplicativeFactor.HasValue)
        {
            while (currentValue <= range.FinalPoint.Value)
            {
                yield return currentValue;
                currentValue *= range.MultiplicativeFactor.Value;
            }
        }
    }
}
