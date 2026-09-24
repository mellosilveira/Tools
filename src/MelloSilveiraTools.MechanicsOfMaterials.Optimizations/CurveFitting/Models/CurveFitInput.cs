namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

public record CurveFitInput
{
    public required double[] InitialParameters { get; init; }
    public required double[] LowerBounds { get; init; }
    public required double[] UpperBounds { get; init; }
    public required List<double[]> IndependentVariables { get; init; }
    public required double[] DependentVariable { get; init; }
    public required Func<double[], double[], double> Calculate { get; init; }
    
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

    public int MaxIterations { get; init; } = CurveFittingConstants.MaxIterations;
    public double Tolerance { get; init; } = CurveFittingConstants.Tolerance;
    public int PopulationMultiplier { get; init; } = 10;
}
