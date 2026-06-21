using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Caching;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.ConstitutiveEquations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.GeometricProperties;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.MechanicsOfMaterials;

/// <summary>
/// Provides extension methods to dependency injection of Tools project.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Mechanics of Materials services (constitutive equations, fatigue and
    /// geometric property calculators) as singletons into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection used to register the dependencies.</param>
    /// <returns>The same service collection so additional registrations can be chained.</returns>
    public static IServiceCollection AddMechanicsOfMaterialsServices(this IServiceCollection services) => services
        // Register service dependencies.
        .AddSingleton<IMechanicalModelTypeCache, MechanicalModelTypeCache>()
        // Register converters.
        .AddSingleton<IMechanicalParameterConverter, MechanicalParameterConverter>()
        // Register calculators.
        // => for constitutive equations.
        .AddSingleton<IConstitutiveEquationsCalculator, ConstitutiveEquationsCalculator>()
        // => for fatigue.
        .AddSingleton<IFatigueCalculator, FatigueCalculator>()
        // => for geometric properties.
        .AddSingleton<IGeometricPropertyCalculator<CircularProfile>, CircularProfileGeometricPropertyCalculator>()
        .AddSingleton<IGeometricPropertyCalculator<RectangularProfile>, RectangularProfileGeometricPropertyCalculator>()
        // => for mechanical models facade.
        .AddSingleton<IMechanicalModelCalculatorFacade, MechanicalModelCalculatorFacade>()
        .AddSingleton<IMechanicalModelCalculator<MechanicalModelInput>, MechanicalModelCalculatorFacade>()
        // => for elastic model.
        .AddSingleton<IMechanicalModelCalculator<ElasticModelInput>, ElasticModelCalculator>()
        .AddSingleton<IElasticModelCalculator, ElasticModelCalculator>()
        // => for linear viscoelastic Maxwell model.
        .AddSingleton<IMechanicalModelCalculator<MaxwellModelInput>, MaxwellModelCalculator>()
        .AddSingleton<IViscoelasticModelCalculator<MaxwellModelInput>, MaxwellModelCalculator>()
        .AddSingleton<IMaxwellModelCalculator, MaxwellModelCalculator>()
        // => for quasi-linear viscoelastic models.
        .AddSingleton<IMechanicalModelCalculator<FungModelInput>, FungModelCalculator>()
        .AddSingleton<IMechanicalModelCalculator<SimplifiedFungModelInput>, SimplifiedFungModelCalculator>()
        .AddSingleton<IViscoelasticModelCalculator<FungModelInput>, FungModelCalculator>()
        .AddSingleton<IFungModelCalculator, FungModelCalculator>()
        .AddSingleton<IViscoelasticModelCalculator<SimplifiedFungModelInput>, SimplifiedFungModelCalculator>()
        .AddSingleton<IQuasiLinearModelCalculator<FungModelInput, ReducedRelaxationFunction>, FungModelCalculator>()
        .AddSingleton<IQuasiLinearModelCalculator<SimplifiedFungModelInput, PronySeries>, SimplifiedFungModelCalculator>()
        .AddSingleton<ISimplifiedFungModelCalculator, SimplifiedFungModelCalculator>()
        // => for non linear viscoelastic Schapery model.
        .AddSingleton<IMechanicalModelCalculator<SchaperyModelInput>, SchaperyModelCalculator>()
        .AddSingleton<IViscoelasticModelCalculator<SchaperyModelInput>, SchaperyModelCalculator>()
        .AddSingleton<ISchaperyModelCalculator, SchaperyModelCalculator>()
        // => for non linear viscoelastic Modified Superposition Method.
        .AddSingleton<IMechanicalModelCalculator<ModifiedSuperpositionMethodInput>, ModifiedSuperpositionMethodCalculator>()
        .AddSingleton<IViscoelasticModelCalculator<ModifiedSuperpositionMethodInput>, ModifiedSuperpositionMethodCalculator>()
        .AddSingleton<IModifiedSuperpositionMethodCalculator, ModifiedSuperpositionMethodCalculator>();
}