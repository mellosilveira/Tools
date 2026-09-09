using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

public record ExperimentalDataProcessingOptions(
    double StartTimeThreshold,
    ushort BufferSize = 10,
    double Tolerance = MathematicConstants.Tolerance,
    double RelativeTolerance = MathematicConstants.RelativeTolerance,
    double RateTolerance = MathematicConstants.Tolerance,
    double AccelerationTolerance = MathematicConstants.Tolerance,
    double SkipTimeStep = 0)
{
    public static readonly ExperimentalDataProcessingOptions Default = new(0);
}
