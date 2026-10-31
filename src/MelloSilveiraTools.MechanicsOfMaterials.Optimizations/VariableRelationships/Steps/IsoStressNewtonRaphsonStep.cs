using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Steps;

/// <summary>
/// Pipeline step that executes a 1D Newton-Raphson numerical root-finding algorithm for each $(n-1)$ tuple
/// to resolve the dependent constitutive parameter $p_k$ satisfying $\sigma(t^*; \mathbf{p}) = \sigma^*$.
/// </summary>
/// <param name="calculatorFactory">Factory resolving mechanical model calculator facades.</param>
/// <param name="differentiation">Numerical differentiation provider used to evaluate finite-difference gradients.</param>
public sealed class IsoStressNewtonRaphsonStep(
    IMechanicalModelCalculatorFactory calculatorFactory,
    IDifferentiation differentiation)
    : ISyncPipelineStep<VariableGridTuple, VariableCombinationPoint>
{
    private const int MaxIterations = 50;

    /// <inheritdoc />
    public string Name => nameof(IsoStressNewtonRaphsonStep);

    /// <inheritdoc />
    public VariableCombinationPoint Execute(VariableGridTuple input)
    {
        ArgumentNullException.ThrowIfNull(input);

        ParameterBound dependentBound = input.DependentBound;
        IMechanicalModelCalculatorFacade calculator = calculatorFactory.CreateCalculatorFacade(input.Input.MechanicalModelName);

        double EvaluateStress(double pkVal)
        {
            Dictionary<string, double> parameters = new(input.FreeParameters.Count + 1);
            foreach (KeyValuePair<string, double> kvp in input.FreeParameters)
            {
                parameters[kvp.Key] = kvp.Value;
            }

            parameters[dependentBound.ParameterName] = pkVal;

            GenericMechanicalModelInput genericInput = ConstitutiveModelInputFactory.CreateInput(input.Input.MechanicalModelName, parameters);
            return calculator.CalculateStress(genericInput, input.Input.TargetTime);
        }

        double targetStress = input.Input.TargetStress;
        double tolerance = input.Input.Tolerance;
        double pk = (dependentBound.MinValue + dependentBound.MaxValue) / 2.0;

        double calculatedStress = EvaluateStress(pk);
        double residual = calculatedStress - targetStress;

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            if (Math.Abs(residual) <= tolerance)
            {
                break;
            }

            double h = Math.Max(1e-6, Math.Abs(pk) * 1e-4);
            double derivative = differentiation.Calculate(EvaluateStress, h, pk);

            if (Math.Abs(derivative) < 1e-12)
            {
                double stepDelta = dependentBound.Step > 0.0 ? dependentBound.Step : 0.01;
                pk += residual < 0.0 ? stepDelta : -stepDelta;
            }
            else
            {
                double delta = residual / derivative;
                if (double.IsNaN(delta) || double.IsInfinity(delta))
                {
                    break;
                }

                double nextPk = pk - delta;
                if (Math.Abs(nextPk - pk) < 1e-9)
                {
                    pk = nextPk;
                    break;
                }

                pk = nextPk;
            }

            calculatedStress = EvaluateStress(pk);
            residual = calculatedStress - targetStress;
        }

        Dictionary<string, double> allParameters = new(input.FreeParameters.Count + 1);
        foreach (KeyValuePair<string, double> kvp in input.FreeParameters)
        {
            allParameters[kvp.Key] = kvp.Value;
        }

        allParameters[dependentBound.ParameterName] = pk;

        return new VariableCombinationPoint(allParameters, calculatedStress, residual);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
