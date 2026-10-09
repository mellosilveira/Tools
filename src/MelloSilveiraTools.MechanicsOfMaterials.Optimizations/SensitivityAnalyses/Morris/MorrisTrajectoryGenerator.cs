using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Morris;

/// <summary>
/// Generates the randomized One-at-a-Time (OAT) trajectories for the Morris Screening method.
/// </summary>
/// <remarks>
/// This service constructs a discretized parameter grid, evaluates mechanical model behaviors,
/// and applies physical bounding mappings for trajectory sampling.
/// </remarks>
public sealed class MorrisTrajectoryGenerator(IMechanicalModelCalculatorFactory calculatorFactory)
{
    /// <summary>
    /// Generates trajectories by discretizing the parameter space and varying one parameter at a time.
    /// </summary>
    /// <param name="input">The payload containing boundaries, levels, and trajectory count.</param>
    /// <returns>A list of generated trajectories, each containing the evaluated points.</returns>
    public List<List<MorrisPoint>> Generate(MorrisInput input)
    {
        List<List<MorrisPoint>> trajectories = [];
        Random random = new();

        int k = input.Boundaries.Count;
        int p = input.Levels;
        int r = input.Trajectories;

        // Step Delta: p / (2 * (p - 1))
        double delta = (double)p / (2.0 * (p - 1.0));

        // The maximum base level index to ensure we can always step +delta without exceeding 1.0
        int deltaLevels = p / 2;
        int maxBaseLevel = (p - 1) - deltaLevels;

        for (int i = 0; i < r; i++)
        {
            List<MorrisPoint> trajectory = [];

            // 1. Generate base point on the normalized grid [0, 1]
            double[] currentNormalized = new double[k];
            for (int j = 0; j < k; j++)
            {
                int baseLevel = random.Next(0, maxBaseLevel + 1);
                currentNormalized[j] = (double)baseLevel / (p - 1.0);
            }

            // 2. Generate random permutation of the parameters
            List<int> permutation = [.. Enumerable.Range(0, k).OrderBy(_ => random.Next())];

            // Evaluate base point
            trajectory.Add(EvaluatePoint(input, currentNormalized));

            // 3. OAT variations
            foreach (int dim in permutation)
            {
                // Step positive since we bounded the base level securely
                currentNormalized[dim] += delta;
                trajectory.Add(EvaluatePoint(input, currentNormalized));
            }

            trajectories.Add(trajectory);
        }

        return trajectories;
    }

    private MorrisPoint EvaluatePoint(MorrisInput input, double[] normalizedParameters)
    {
        Dictionary<string, double> physicalParameters = [];
        JsonObject? configNode = JsonSerializer.SerializeToNode(input.BaselineConfiguration)?.AsObject();

        if (configNode is null)
        {
            throw new InvalidOperationException("Failed to serialize the baseline configuration.");
        }

        for (int i = 0; i < input.Boundaries.Count; i++)
        {
            MorrisParameterBoundary boundary = input.Boundaries.ElementAt(i);
            double min = boundary.Range.InitialPoint;
            double max = boundary.Range.FinalPoint.Value; //TODO: AJUSTAR

            double physicalValue = min + (normalizedParameters[i] * (max - min));
            physicalParameters[boundary.ParameterPath] = physicalValue;

            int openBracket = boundary.ParameterPath.IndexOf('[');
            if (openBracket > -1)
            {
                string propName = boundary.ParameterPath[..openBracket];
                string indexStr = boundary.ParameterPath.Substring(openBracket + 1, boundary.ParameterPath.Length - openBracket - 2);
                int index = int.Parse(indexStr);

                JsonArray? arrayNode = configNode[propName]?.AsArray();
                if (arrayNode is not null)
                {
                    arrayNode[index] = physicalValue;
                }
            }
            else
            {
                configNode[boundary.ParameterPath] = physicalValue;
            }
        }

        ConstitutiveParameters? mutatedConfig = configNode.Deserialize(input.BaselineConfiguration.GetType()) as ConstitutiveParameters;

        if (mutatedConfig is null)
        {
            throw new InvalidOperationException("Failed to deserialize the mutated configuration.");
        }

        GenericMechanicalModelInput simulationInput = new(
            new MechanicalModelInput
            {
                MechanicalModelName = input.MechanicalModelName,
                TimeStep = 1.0
            },
            mutatedConfig);

        IMechanicalModelCalculatorFacade facade = calculatorFactory.CreateCalculatorFacade(input.MechanicalModelName, simulationInput);

        Dictionary<string, double> outputs = [];
        foreach (string targetOutput in input.TargetOutputs)
        {
            double evaluatedValue = targetOutput.ToUpperInvariant() switch
            {
                "STRESS" => facade.CalculateStress(simulationInput, 1.0, 1.0),
                "STRAIN" => facade.CalculateStrain(simulationInput, 1.0, 1.0),
                "FORCE" => facade.CalculateForce(simulationInput, 1.0, 1.0),
                "DISPLACEMENT" => facade.CalculateDisplacement(simulationInput, 1.0, 1.0),
                _ => throw new InvalidOperationException($"Unsupported target output: {targetOutput}")
            };
            outputs[targetOutput] = evaluatedValue;
        }

        return new MorrisPoint(physicalParameters, outputs);
    }
}
