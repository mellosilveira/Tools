namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;

public record CurveFitResultData
{
    // TODO: CRIAR CONSTRUTOR BASEADO NO USO.

    public bool IsSuccessful { get; init; }
    public double FinalError { get; init; }
    public string? Message { get; init; } = null;
    public required ParameterGroupResultData[] ParameterGroups { get; init; }
}
