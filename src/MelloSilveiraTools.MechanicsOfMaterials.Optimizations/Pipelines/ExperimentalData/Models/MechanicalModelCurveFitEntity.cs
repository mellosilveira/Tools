using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Database entity for mechanical model curve fits.
/// </summary>
[Table("mechanical_model_curve_fits")]
public record MechanicalModelCurveFitEntity : EntityBase
{
    [UniqueColumn]
    [Column]
    public required string Identifier { get; init; }

    [Column]
    public required string MechanicalModelName { get; init; }

    [Column]
    public required double InitialAcceptedRange { get; init; }

    [Column]
    public required double FinalAcceptedRange { get; init; }

    [Column]
    public required MechanicalBehaviorType MechanicalBehaviorType { get; init; }

    [Column]
    public required RampTimeConsideration RampTimeConsideration { get; init; }

    [Column]
    public required ViscoelasticEffect ViscoelasticEffect { get; init; }

    [Column]
    public required decimal Error { get; init; }

    [Column]
    public required int Iterations { get; init; }

    [Column]
    public required string ConstitutiveParameters { get; init; }
}
