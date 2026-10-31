using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Steps;

/// <summary>
/// Pipeline filter step that validates and retains parameter combinations whose dependent and free
/// parameter values fall within the designated bounds and whose absolute stress residual meets the tolerance threshold.
/// </summary>
public sealed class IsoStressFilterStep : IEnumerablePipelineStep<VariableCombinationPoint, VariableCombinationPoint>
{
    private readonly IReadOnlyDictionary<string, ParameterBound> _parameterBounds;
    private readonly double _tolerance;

    /// <summary>
    /// Initializes a new instance of the <see cref="IsoStressFilterStep"/> class with parameter bounds list and tolerance.
    /// </summary>
    /// <param name="parameterBounds">The collection of parameter boundary definitions.</param>
    /// <param name="tolerance">The maximum allowable absolute residual tolerance.</param>
    public IsoStressFilterStep(IReadOnlyList<ParameterBound> parameterBounds, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(parameterBounds);
        Dictionary<string, ParameterBound> dict = new(parameterBounds.Count, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < parameterBounds.Count; i++)
        {
            dict[parameterBounds[i].ParameterName] = parameterBounds[i];
        }

        _parameterBounds = dict;
        _tolerance = tolerance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IsoStressFilterStep"/> class with parameter bounds dictionary and tolerance.
    /// </summary>
    /// <param name="parameterBounds">The dictionary of parameter boundary definitions mapped by parameter name.</param>
    /// <param name="tolerance">The maximum allowable absolute residual tolerance.</param>
    public IsoStressFilterStep(IReadOnlyDictionary<string, ParameterBound> parameterBounds, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(parameterBounds);
        _parameterBounds = parameterBounds;
        _tolerance = tolerance;
    }

    /// <inheritdoc />
    public string Name => nameof(IsoStressFilterStep);

    /// <summary>
    /// Evaluates whether a parameter combination point satisfies boundary and residual criteria.
    /// </summary>
    /// <param name="point">The evaluated combination point to validate.</param>
    /// <returns><c>true</c> if valid; otherwise, <c>false</c>.</returns>
    public bool Filter(VariableCombinationPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        if (Math.Abs(point.Residual) > _tolerance + 1e-9)
        {
            return false;
        }

        foreach ((string name, double value) in point.Parameters)
        {
            if (_parameterBounds.TryGetValue(name, out ParameterBound? bound))
            {
                if (value < bound.MinValue - 1e-9 || value > bound.MaxValue + 1e-9)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc />
    public IEnumerable<VariableCombinationPoint> Execute(VariableCombinationPoint input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Filter(input))
        {
            yield return input;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
