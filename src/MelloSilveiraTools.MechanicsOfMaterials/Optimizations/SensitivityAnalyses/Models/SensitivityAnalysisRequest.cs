using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

public record SensitivityAnalysisRequest
{
    public ConstitutiveParameters Baseline { get; init; }
    public Dictionary<string, RangeParameters> ConstantVariables { get; init; }
    public Dictionary<string, Dictionary<int, RangeParameters>> ArrayVariables { get; init; }
}
