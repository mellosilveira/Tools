using MelloSilveiraTools.MechanicsOfMaterials.ConstitutiveEquations;
using MelloSilveiraTools.MechanicsOfMaterials.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.GeometricProperties;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.MechanicsOfMaterials;

/// <summary>
/// Provides extension methods to dependency injection of Tools project.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Register services for Mechanics of Materials.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMechanicsOfMaterialsServices(this IServiceCollection services)
        => services
            .AddSingleton<IConstitutiveEquationsCalculator, ConstitutiveEquationsCalculator>()
            .AddSingleton<IFatigueCalculator, FatigueCalculator>()
            // Register geometric properties.
            .AddSingleton<IGeometricPropertyCalculator<CircularProfile>, CircularProfileGeometricPropertyCalculator>()
            .AddSingleton<IGeometricPropertyCalculator<RectangularProfile>, RectangularProfileGeometricPropertyCalculator>();
}