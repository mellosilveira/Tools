using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using System.Reflection;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Encapsulates the boundary values and comparative deltas between the initial and final simulation outputs.
/// </summary>
/// <param name="InitialOutput">The output produced at the initial simulation step.</param>
/// <param name="FinalOutput">The output produced at the final simulation step.</param>
/// <param name="AbsoluteDelta">The absolute difference between final and initial outputs.</param>
/// <param name="PercentageDelta">The relative percentage change between final and initial outputs.</param>
public sealed record SimulationDeltaOutput(
    MechanicalModelOutput InitialOutput,
    MechanicalModelOutput FinalOutput,
    MechanicalModelOutput AbsoluteDelta,
    MechanicalModelOutput PercentageDelta)
{
    /// <summary>
    /// Computes the absolute and percentage deltas across all writable numeric properties
    /// of the resolved <see cref="MechanicalModelOutput"/> hierarchy.
    /// </summary>
    /// <param name="mechanicalModelTypeResolver">Type resolver identifying the concrete <see cref="MechanicalModelOutput"/> type.</param>
    /// <param name="initialOutput">The output produced at the initial simulation step.</param>
    /// <param name="lastOutput">The output produced at the final simulation step.</param>
    /// <returns>A <see cref="SimulationDeltaOutput"/> containing initial, final, absolute delta, and percentage delta outputs.</returns>
    public static SimulationDeltaOutput Calculate(
        IMechanicalModelTypeResolver mechanicalModelTypeResolver,
        MechanicalModelOutput initialOutput,
        MechanicalModelOutput lastOutput)
    {
        MechanicalModelOutput absoluteDelta = (MechanicalModelOutput)Activator.CreateInstance(mechanicalModelTypeResolver.Output)!;
        MechanicalModelOutput percentageDelta = (MechanicalModelOutput)Activator.CreateInstance(mechanicalModelTypeResolver.Output)!;

        foreach (PropertyInfo property in mechanicalModelTypeResolver.Output.GetPropertiesInHierarchy())
        {
            if (!property.CanWrite)
                continue;

            double? initialValue = (double?)property.GetValue(initialOutput);
            double? lastValue = (double?)property.GetValue(lastOutput);

            if (initialValue.HasValue && lastValue.HasValue)
            {
                double absoluteDeltaValue = lastValue.Value - initialValue.Value;
                property.SetValue(absoluteDelta, absoluteDeltaValue);

                double percentage = initialValue.Value != 0.0 ? 100 * absoluteDeltaValue / initialValue.Value : 0.0;
                property.SetValue(percentageDelta, percentage);
            }
        }

        return new SimulationDeltaOutput(initialOutput, lastOutput, absoluteDelta, percentageDelta);
    }
}
