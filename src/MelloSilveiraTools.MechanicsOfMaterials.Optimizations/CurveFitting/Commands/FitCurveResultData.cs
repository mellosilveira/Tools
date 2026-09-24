namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;

public record FitCurveResultData
{
    // TODO: CRIAR CONSTRUTOR BASEADO NO USO.
    public double FinalError { get; init; }
    public required ParameterGroupResultData[] ParameterGroups { get; init; }
}
