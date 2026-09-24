namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

public record MathematicalCurveFitInput
{
    public required int NumberOfParameters { get; init; }
    public required double[] IndependentVariable { get; init; }
    public required double[] DependentVariable { get; init; }
    public bool ZeroBased { get; init; } = false;
    public double Tolerance { get; init; } = CurveFittingConstants.Tolerance;
    public int MaxIterations { get; init; } = CurveFittingConstants.MaxIterations;
    public double[]? LowerBounds { get; init; }
    public double[]? UpperBounds { get; init; }
    public double[]? InitialParameters { get; init; }

    /// <summary>
    /// Perfil de otimização selecionado (padrão: Automatic).
    /// </summary>
    public CurveFitProfile Profile { get; init; } = CurveFitProfile.Automatic;

    /// <summary>
    /// Motor de regras booleanas de curto-circuito.
    /// Retorne false para combinações fisicamente ou comercialmente inadmissíveis.
    /// </summary>
    public Func<double[], bool>? ValidateParameters { get; init; }

    /// <summary>
    /// Meta dinâmica de qualidade do ajuste (Coeficiente de Determinação).
    /// </summary>
    public double? TargetRSquared { get; init; }
}
