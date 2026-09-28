using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Converters;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that generates a unique deterministic identifier for the curve-fitted parameters.
/// </summary>
public class IdentifierBuilderStep : ISyncPipelineStep<MechanicalModelCurveFitOutput, string>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new SignificantFiguresDoubleJsonConverter(7) } };

    /// <inheritdoc />
    public string Name => nameof(IdentifierBuilderStep);

    /// <inheritdoc />
    public string Execute(MechanicalModelCurveFitOutput output)
    {
        string constitutiveParamsJson = JsonSerializer.Serialize((object)output.ConstitutiveParameters, JsonOptions);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(constitutiveParamsJson));
        string identifierHash = Convert.ToHexString(bytes);
        return identifierHash;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
