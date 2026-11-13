using System.Diagnostics.CodeAnalysis;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;

/// <summary>
/// Database entity for mechanical model curve fit outputs.
/// </summary>
[Table("mechanical_model_curve_fit_outputs")]
public record MechanicalModelCurveFitOutputEntity : EntityBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalModelCurveFitOutputEntity"/> for persistence mapping and deserialization.
    /// </summary>
    [SetsRequiredMembers]
    public MechanicalModelCurveFitOutputEntity()
    {
        Identifier = string.Empty;
        MechanicalModelName = string.Empty;
        ConstitutiveParameters = string.Empty;
    }

    [UniqueColumn]
    public required string Identifier { get; init; }

    [Column]
    public string? ExtenalIdentifier { get; init; }

    [Column]
    public required string MechanicalModelName { get; init; }

    [Column]
    public required double InitialAcceptedRange { get; init; }

    [Column]
    public required double FinalAcceptedRange { get; init; }

    [Column]
    public required LoadResponseRelationship LoadResponseRelationship { get; init; }

    [Column]
    public required RampTimeConsideration RampTimeConsideration { get; init; }

    [Column]
    public required ViscoelasticEffect ViscoelasticEffect { get; init; }

    [Column]
    public required double Error { get; init; }

    [Column]
    public required int Iterations { get; init; }

    [Column]
    public required string ConstitutiveParameters { get; init; }
}
