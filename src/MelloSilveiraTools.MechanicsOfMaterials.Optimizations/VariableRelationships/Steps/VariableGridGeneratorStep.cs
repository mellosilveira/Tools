using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Steps;

/// <summary>
/// Pipeline step that discretizes $n-1$ constitutive parameters into a Cartesian product grid
/// while designating the most sensitive parameter ($p_k$) as the dependent variable for subsequent 1D root-solving.
/// </summary>
/// <param name="calculatorFactory">Factory resolving mechanical model calculator facades.</param>
/// <param name="differentiation">Numerical differentiation provider used to evaluate parameter sensitivity gradients.</param>
public sealed class VariableGridGeneratorStep(
    IMechanicalModelCalculatorFactory calculatorFactory,
    IDifferentiation differentiation)
    : IEnumerablePipelineStep<VariableRelationshipInput, VariableGridTuple>
{
    /// <inheritdoc />
    public string Name => nameof(VariableGridGeneratorStep);

    /// <inheritdoc />
    public IEnumerable<VariableGridTuple> Execute(VariableRelationshipInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<ParameterBound> bounds = input.ParameterBounds;
        if (bounds.Count == 0)
        {
            yield break;
        }

        if (bounds.Count == 1)
        {
            Dictionary<string, double> emptyFreeParams = [];
            yield return new VariableGridTuple(emptyFreeParams, bounds[0], bounds, input);
            yield break;
        }

        // 1. Identify the dependent parameter p_k by finding the one with maximum sensitivity at interval midpoint
        Dictionary<string, double> midpoints = new(bounds.Count);
        for (int i = 0; i < bounds.Count; i++)
        {
            ParameterBound b = bounds[i];
            midpoints[b.ParameterName] = (b.MinValue + b.MaxValue) / 2.0;
        }

        IMechanicalModelCalculatorFacade calculator = calculatorFactory.CreateCalculatorFacade(input.MechanicalModelName);

        int bestIndex = 0;
        double maxSensitivity = -1.0;

        for (int i = 0; i < bounds.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ParameterBound bound = bounds[i];
            double midVal = midpoints[bound.ParameterName];
            double h = Math.Max(1e-6, Math.Abs(midVal) * 1e-4);

            double EvaluateAt(double val)
            {
                Dictionary<string, double> currentParams = new(midpoints);
                currentParams[bound.ParameterName] = val;
                GenericMechanicalModelInput genericInput = ConstitutiveModelInputFactory.CreateInput(input.MechanicalModelName, currentParams);
                return calculator.CalculateStress(genericInput, input.TargetTime);
            }

            double derivative = differentiation.Calculate(EvaluateAt, h, midVal);
            double sensitivity = Math.Abs(derivative);
            if (sensitivity > maxSensitivity)
            {
                maxSensitivity = sensitivity;
                bestIndex = i;
            }
        }

        ParameterBound dependentBound = bounds[bestIndex];

        // 2. The remaining n-1 bounds are the free parameters
        List<ParameterBound> freeBounds = [];
        for (int i = 0; i < bounds.Count; i++)
        {
            if (i != bestIndex)
            {
                freeBounds.Add(bounds[i]);
            }
        }

        // 3. Discretize each free parameter within [MinValue, MaxValue] with positive step increment
        List<List<double>> valueLists = [];
        for (int j = 0; j < freeBounds.Count; j++)
        {
            ParameterBound fb = freeBounds[j];
            List<double> values = [];
            double step = fb.Step > 0.0 ? fb.Step : (fb.MaxValue - fb.MinValue);
            if (step <= 0.0)
            {
                step = 1.0;
            }

            for (double v = fb.MinValue; v <= fb.MaxValue + 1e-9; v += step)
            {
                values.Add(v);
            }

            if (values.Count == 0)
            {
                values.Add(fb.MinValue);
            }

            valueLists.Add(values);
        }

        // 4. Generate Cartesian product of the n-1 free parameters
        int m = freeBounds.Count;
        int[] indices = new int[m];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<string, double> currentTuple = new(m);
            for (int j = 0; j < m; j++)
            {
                currentTuple[freeBounds[j].ParameterName] = valueLists[j][indices[j]];
            }

            yield return new VariableGridTuple(currentTuple, dependentBound, bounds, input);

            int p = m - 1;
            while (p >= 0)
            {
                indices[p]++;
                if (indices[p] < valueLists[p].Count)
                {
                    break;
                }

                indices[p] = 0;
                p--;
            }

            if (p < 0)
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
