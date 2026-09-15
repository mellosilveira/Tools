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
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Mechanics of Materials services (constitutive equations, fatigue and
        /// geometric property calculators) as singletons into the dependency injection container.
        /// </summary>
        /// <returns>The same service collection so additional registrations can be chained.</returns>
        public IServiceCollection AddMechanicsOfMaterialsServices(bool addMechanicalModels = false)
        {
            if (addMechanicalModels)
                services.AddMechanicalModels();

            return services
                // Register converters.
                .AddSingleton<IMechanicalParameterConverter, MechanicalParameterConverter>()
                // Register calculators.
                .AddSingleton<IConstitutiveEquationsCalculator, ConstitutiveEquationsCalculator>()
                .AddSingleton<IFatigueCalculator, FatigueCalculator>()
                // Register geometric properties.
                .AddSingleton<IGeometricPropertyCalculator<CircularProfile>, CircularProfileGeometricPropertyCalculator>()
                .AddSingleton<IGeometricPropertyCalculator<RectangularProfile>, RectangularProfileGeometricPropertyCalculator>()
                // Register service dependencies.
                .AddSingleton<ServiceLocator>()
                .AddSingleton<IMechanicalModelTypeCache, MechanicalModelTypeCache>();
        }

        public IServiceCollection AddMechanicalModels() => services
            // Register elastic model.
            .AddSingleton<IMechanicalModelCalculator<ElasticConstitutiveParameters>, ElasticModelCalculator>()
            .AddSingleton<IElasticModelCalculator, ElasticModelCalculator>()
            // Register linear viscoelastic models.
            .AddSingleton<IMechanicalModelCalculator<MaxwellConstitutiveParameters>, MaxwellModelCalculator>()
            .AddSingleton<IViscoelasticModelCalculator<MaxwellConstitutiveParameters>, MaxwellModelCalculator>()
            .AddSingleton<IMaxwellModelCalculator, MaxwellModelCalculator>()
            // Register quasi-linear viscoelastic models.
            .AddSingleton<IMechanicalModelCalculator<FungConstitutiveParameters>, FungModelCalculator>()
            .AddSingleton<IMechanicalModelCalculator<SimplifiedFungConstitutiveParameters>, SimplifiedFungModelCalculator>()
            .AddSingleton<IViscoelasticModelCalculator<FungConstitutiveParameters>, FungModelCalculator>()
            .AddSingleton<IViscoelasticModelCalculator<SimplifiedFungConstitutiveParameters>, SimplifiedFungModelCalculator>()
            .AddSingleton<IQuasiLinearModelCalculator<FungConstitutiveParameters, ReducedRelaxationFunction>, FungModelCalculator>()
            .AddSingleton<IQuasiLinearModelCalculator<SimplifiedFungConstitutiveParameters, PronySeries>, SimplifiedFungModelCalculator>()
            .AddSingleton<IFungModelCalculator, FungModelCalculator>()
            .AddSingleton<ISimplifiedFungModelCalculator, SimplifiedFungModelCalculator>()
            // Register nonlinear viscoelastic models.
            .AddSingleton<IMechanicalModelCalculator<ModifiedSuperpositionMethodConstitutiveParameters>, ModifiedSuperpositionMethodCalculator>()
            .AddSingleton<IMechanicalModelCalculator<SchaperyConstitutiveParameters>, SchaperyModelCalculator>()
            .AddSingleton<IViscoelasticModelCalculator<ModifiedSuperpositionMethodConstitutiveParameters>, ModifiedSuperpositionMethodCalculator>()
            .AddSingleton<IViscoelasticModelCalculator<SchaperyConstitutiveParameters>, SchaperyModelCalculator>()
            .AddSingleton<IModifiedSuperpositionMethodCalculator, ModifiedSuperpositionMethodCalculator>()
            .AddSingleton<ISchaperyModelCalculator, SchaperyModelCalculator>();
    }
}