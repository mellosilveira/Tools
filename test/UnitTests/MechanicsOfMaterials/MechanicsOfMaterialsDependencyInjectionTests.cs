using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Caching;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.ConstitutiveEquations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.GeometricProperties;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.MechanicsOfMaterials;

/// <summary>
/// Unit tests for <see cref="DependencyInjection"/> in <c>MelloSilveiraTools.MechanicsOfMaterials</c>.
/// </summary>
public sealed class MechanicsOfMaterialsDependencyInjectionTests
{
    private static ServiceCollection CreateBaseServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddCoreServices(new ResiliencePipelineSettings
        {
            BackoffType = Polly.DelayBackoffType.Constant,
            DelayInMilliseconds = 10,
            MaxRetryAttempts = 1,
            UseJitter = false
        });
        services.AddMathematicsServices();
        return services;
    }

    [Fact]
    public void AddMechanicsOfMaterialsServices_WithoutAndWithModels_RegistersExpectedServices()
    {
        ServiceCollection withoutModels = CreateBaseServices();
        withoutModels.AddMechanicsOfMaterialsServices(addMechanicalModels: false);
        using ServiceProvider providerWithoutModels = withoutModels.BuildServiceProvider();

        Assert.NotNull(providerWithoutModels.GetRequiredService<IMechanicalParameterConverter>());
        Assert.NotNull(providerWithoutModels.GetRequiredService<IConstitutiveEquationsCalculator>());
        Assert.NotNull(providerWithoutModels.GetRequiredService<IFatigueCalculator>());
        Assert.NotNull(providerWithoutModels.GetRequiredService<IGeometricPropertyCalculator<CircularProfile>>());
        Assert.NotNull(providerWithoutModels.GetRequiredService<IGeometricPropertyCalculator<RectangularProfile>>());
        Assert.NotNull(providerWithoutModels.GetRequiredService<IMechanicalModelTypeCache>());
        Assert.NotNull(providerWithoutModels.GetRequiredService<IMechanicalModelCalculatorFactory>());
        Assert.NotNull(providerWithoutModels.GetRequiredKeyedService<IMechanicalModelTypeResolver>(nameof(MechanicalModel.Elastic)));
        Assert.Null(providerWithoutModels.GetService<IElasticModelCalculator>());

        ServiceCollection withModels = CreateBaseServices();
        withModels.AddMechanicsOfMaterialsServices(addMechanicalModels: true);
        using ServiceProvider providerWithModels = withModels.BuildServiceProvider();
        Assert.IsType<ElasticModelCalculator>(providerWithModels.GetRequiredService<IElasticModelCalculator>());
    }
}
