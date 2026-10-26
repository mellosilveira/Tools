using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Security.Cryptography;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that generates a unique deterministic identifier for the curve-fitted parameters.
/// </summary>
public sealed class CurveFitOutputIdentifierBuilderStep : ISyncPipelineStep<MechanicalModelCurveFitOutput, string>
{
    /// <inheritdoc />
    public string Name => nameof(CurveFitOutputIdentifierBuilderStep);

    /// <inheritdoc />
    public string Execute(MechanicalModelCurveFitOutput output)
    {
        string constitutiveParamsJson = OptimizationJsonOptions.Serialize(output.ConstitutiveParameters);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(constitutiveParamsJson));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
