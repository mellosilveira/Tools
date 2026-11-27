using System.Text.Json.Nodes;

namespace MelloSilveiraTools.MechanicsOfMaterials.Commands.MechanicalModels.Get;

/// <summary>
/// Metadata describing a registered mechanical model, its calculator type,
/// and representative example JSON payloads for its constitutive parameters and output models.
/// </summary>
/// <param name="MechanicalModelName">Registered mechanical model name.</param>
/// <param name="CalculatorTypeName">Name of the calculator service type.</param>
/// <param name="ConstitutiveParameters">Representative example JSON payload for the constitutive parameters type.</param>
/// <param name="Output">Representative example JSON payload for the model output type.</param>
public sealed record MechanicalModelMetadata(
    string MechanicalModelName,
    string CalculatorTypeName,
    JsonNode ConstitutiveParameters,
    JsonNode Output);
