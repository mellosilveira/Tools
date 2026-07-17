using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace SoftTissue.Domain.Models.Optimization
{
    public record CurveFitInput
    {
        public MechanicalModelInput InitialInput { get; init; }
        public double[] TimePoints { get; init; }
        public double[] ExperimentalStress { get; init; }
        public OptimizationOptions Options { get; init; }
        public double Strain { get; init; }
        public Func<MechanicalModelInput, double[], double> EvaluateConstraintsAndPenalties { get; init; }
    }

    public record CurveFitResult(
        bool IsSuccessful,
        double[] OptimizedParameters,
        double FinalError,
        string Message
    );
}
