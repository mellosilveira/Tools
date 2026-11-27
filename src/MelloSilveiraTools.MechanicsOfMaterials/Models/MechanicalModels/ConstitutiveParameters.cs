using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Represents the base type for all constitutive parameters used in mechanical models.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ElasticConstitutiveParameters), nameof(MechanicalModel.Elastic))]
[JsonDerivedType(typeof(MaxwellConstitutiveParameters), nameof(MechanicalModel.Maxwell))]
[JsonDerivedType(typeof(FungConstitutiveParameters), nameof(MechanicalModel.Fung))]
[JsonDerivedType(typeof(SimplifiedFungConstitutiveParameters), nameof(MechanicalModel.SimplifiedFung))]
[JsonDerivedType(typeof(SchaperyConstitutiveParameters), nameof(MechanicalModel.Schapery))]
[JsonDerivedType(typeof(ModifiedSuperpositionMethodConstitutiveParameters), nameof(MechanicalModel.ModifiedSuperpositionMethod))]
public abstract record ConstitutiveParameters;