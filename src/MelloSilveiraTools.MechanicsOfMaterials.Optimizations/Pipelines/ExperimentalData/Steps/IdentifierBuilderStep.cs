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
public class IdentifierBuilderStep : ISyncPipelineStep<MechanicalModelCurveFitOutput, (string OutputIdentifier, MechanicalModelCurveFitOutput CurveFit)>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new SignificantFiguresDoubleJsonConverter(7) } };

    /// <inheritdoc />
    public string Name => nameof(IdentifierBuilderStep);

    /// <inheritdoc />
    public (string OutputIdentifier, MechanicalModelCurveFitOutput CurveFit) Execute(MechanicalModelCurveFitOutput output)
    {
        string constitutiveParamsJson = JsonSerializer.Serialize((object)output.ConstitutiveParameters, JsonOptions);
        string identifierHash = ComputeSha256Hash(constitutiveParamsJson);
        return (identifierHash, output);
    }

    private static string ComputeSha256Hash(string rawData)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
