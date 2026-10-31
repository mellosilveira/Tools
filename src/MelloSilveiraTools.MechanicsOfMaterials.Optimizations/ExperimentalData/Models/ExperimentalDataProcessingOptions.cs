using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;

public record ExperimentalDataProcessingOptions(
    ushort BufferSize = 10,
    double Tolerance = MathematicConstants.Tolerance,
    double RelativeTolerance = MathematicConstants.RelativeTolerance,
    double RateTolerance = MathematicConstants.Tolerance,
    double AccelerationTolerance = MathematicConstants.Tolerance);