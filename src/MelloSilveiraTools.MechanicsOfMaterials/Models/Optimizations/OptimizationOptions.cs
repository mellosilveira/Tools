using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations;

public record OptimizationOptions(double[] InitialGuesses, double[] LowerBounds, double[] UpperBounds, int MaxIterations = 1000, double Tolerance = MathematicConstants.Tolerance);
