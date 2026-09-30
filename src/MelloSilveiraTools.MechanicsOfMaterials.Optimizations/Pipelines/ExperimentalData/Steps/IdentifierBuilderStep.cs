using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that generates a unique deterministic identifier for the curve-fitted parameters.
/// </summary>
public sealed class IdentifierBuilderStep : ISyncPipelineStep<MechanicalModelCurveFitOutput, string>
{
    /// <inheritdoc />
    public string Name => nameof(IdentifierBuilderStep);

    /// <inheritdoc />
    public string Execute(MechanicalModelCurveFitOutput output)
    {
        ConstitutiveParameters constitutiveParameters = output.ConstitutiveParameters;
        string constitutiveParamsJson = JsonSerializer.Serialize(constitutiveParameters, constitutiveParameters.GetType(), OptimizationJsonOptions.SignificantFigures7);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(constitutiveParamsJson));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
