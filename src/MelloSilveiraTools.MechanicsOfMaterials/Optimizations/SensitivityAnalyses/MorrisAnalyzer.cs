using MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses;

public class MorrisAnalyzer
{
    /// <summary>
    /// Analyzes the trajectories and calculates Mu, MuStar, and Sigma.
    /// </summary>
    /// <param name="levels">The 'p' grid levels used during generation.</param>
    /// <param name="targetOutputs">List of mechanical outputs (e.g., "Stress").</param>
    /// <param name="parameterPaths">List of parameters analyzed (e.g., "N", "A[0]").</param>
    /// <param name="trajectories">The completed simulation points grouped by trajectory.</param>
    public MorrisOutput Analyze(
        int levels,
        IReadOnlyCollection<string> targetOutputs,
        IReadOnlyCollection<string> parameterPaths,
        List<List<MorrisPoint>> trajectories)
    {
        // The normalized jump size used during generation
        double delta = (double)levels / (2 * (levels - 1));

        // Setup a dictionary to collect Elementary Effects: [Parameter][Output] -> List of EEs
        var elementaryEffects = new Dictionary<string, Dictionary<string, List<double>>>();
        foreach (var param in parameterPaths)
        {
            elementaryEffects[param] = new Dictionary<string, List<double>>();
            foreach (var output in targetOutputs)
            {
                elementaryEffects[param][output] = new List<double>();
            }
        }

        // 1. Calculate Elementary Effects (EE)
        foreach (var trajectory in trajectories)
        {
            // A trajectory has k+1 points. Iterate through the steps to find the deltas.
            for (int i = 0; i < trajectory.Count - 1; i++)
            {
                var point1 = trajectory[i];
                var point2 = trajectory[i + 1];

                string changedParam = null;
                double sign = 1.0;

                // Identify which parameter stepped to calculate the delta sign
                foreach (var param in parameterPaths)
                {
                    double p1 = point1.Parameters[param];
                    double p2 = point2.Parameters[param];

                    // Floating point safe comparison
                    if (Math.Abs(p1 - p2) > 1e-10)
                    {
                        changedParam = param;
                        // If the physical value increased, the normalized step was +Delta
                        sign = p2 > p1 ? 1.0 : -1.0;
                        break;
                    }
                }

                if (changedParam == null) continue;

                // Calculate EE for every requested output based on this parameter step
                foreach (var output in targetOutputs)
                {
                    double y1 = point1.Outputs[output];
                    double y2 = point2.Outputs[output];

                    double deltaY = y2 - y1;

                    // EE = (Change in Output) / (Normalized Change in Parameter)
                    double ee = deltaY / (sign * delta);

                    elementaryEffects[changedParam][output].Add(ee);
                }
            }
        }

        // 2. Calculate Final Metrics (Mu, Mu*, Sigma)
        var results = new List<MorrisMetrics>();

        foreach (var param in parameterPaths)
        {
            foreach (var output in targetOutputs)
            {
                var ees = elementaryEffects[param][output];

                if (ees.Count == 0) continue;

                // Mu: Standard Average
                double mu = ees.Average();

                // MuStar: Absolute Average (Overall Influence)
                double muStar = ees.Average(Math.Abs);

                // Sigma: Sample Standard Deviation (Interaction/Non-linearity)
                double sumOfSquares = ees.Sum(ee => Math.Pow(ee - mu, 2));
                double sigma = ees.Count > 1
                    ? Math.Sqrt(sumOfSquares / (ees.Count - 1))
                    : 0.0;

                results.Add(new MorrisMetrics
                {
                    ParameterPath = param,
                    TargetOutput = output,
                    Mu = mu,
                    MuStar = muStar,
                    Sigma = sigma
                });
            }
        }

        return new MorrisOutput { Results = results };
    }
}