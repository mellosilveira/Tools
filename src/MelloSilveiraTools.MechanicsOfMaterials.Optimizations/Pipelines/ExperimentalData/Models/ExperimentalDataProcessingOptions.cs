using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

public record ExperimentalDataProcessingOptions(
    double SkipTimeStep = 0,
    ushort BufferSize = 10,
    double Tolerance = MathematicConstants.Tolerance,
    double RelativeTolerance = MathematicConstants.RelativeTolerance,
    double RateTolerance = MathematicConstants.Tolerance,
    double AccelerationTolerance = MathematicConstants.Tolerance);