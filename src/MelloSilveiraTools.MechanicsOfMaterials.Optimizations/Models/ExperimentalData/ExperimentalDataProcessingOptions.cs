using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

public record ExperimentalDataProcessingOptions(
    double StartTimeThreshold,
    ushort BufferSize = 10,
    double Tolerance = MathematicConstants.Tolerance,
    double RelativeTolerance = MathematicConstants.RelativeTolerance,
    double DerivativeTolerance = MathematicConstants.Tolerance,
    double SkipTimeStep = 0);
