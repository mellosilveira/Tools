using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.MechanicsOfMaterials.Caching;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.ConstitutiveEquations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.GeometricProperties;
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
    public static IServiceCollection AddMechanicsOfMaterialsServices(this IServiceCollection services)
        => services
            .AddSingleton<IConstitutiveEquationsCalculator, ConstitutiveEquationsCalculator>()
            .AddSingleton<IFatigueCalculator, FatigueCalculator>()
            // Register geometric properties.
            .AddSingleton<IGeometricPropertyCalculator<CircularProfile>, CircularProfileGeometricPropertyCalculator>()
            .AddSingleton<IGeometricPropertyCalculator<RectangularProfile>, RectangularProfileGeometricPropertyCalculator>()
            // Register service dependencies.
            .AddSingleton<ServiceLocator>()
            .AddSingleton<IMechanicalModelTypeCache, MechanicalModelTypeCache>();
}