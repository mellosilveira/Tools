using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations
{
    public record CurveFitInput
    {
        public GenericMechanicalModelInput InitialInput { get; init; }
        public double[] TimePoints { get; init; }
        public double[] ExperimentalStress { get; init; }
        public OptimizationOptions Options { get; init; }
        public double Strain { get; init; }
        public Func<GenericMechanicalModelInput, double[], double> EvaluateConstraintsAndPenalties { get; init; }
    }

    public record CurveFitResult(
        bool IsSuccessful,
        double[] OptimizedParameters,
        double FinalError,
        string Message
    );
}
