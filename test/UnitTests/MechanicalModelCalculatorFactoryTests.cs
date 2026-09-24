using MelloSilveiraTools.Core;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests;

/// <summary>
/// Unit tests for <see cref="IMechanicalModelCalculatorFactory"/> and <see cref="MechanicalModelCalculatorFactory"/>.
/// </summary>
public class MechanicalModelCalculatorFactoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMechanicalModelCalculatorFactory _factory;

    public MechanicalModelCalculatorFactoryTests()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        CoreDependencyInjection.AddCoreServices(services, null, null, null, null, false);
        services.AddMathematicsServices();
        services.AddMechanicsOfMaterialsServices(addMechanicalModels: true);

        _serviceProvider = services.BuildServiceProvider();
        _factory = _serviceProvider.GetRequiredService<IMechanicalModelCalculatorFactory>();
    }

    [Theory]
    [InlineData(nameof(MechanicalModel.Elastic))]
    [InlineData(nameof(MechanicalModel.Maxwell))]
    [InlineData(nameof(MechanicalModel.Fung))]
    [InlineData(nameof(MechanicalModel.SimplifiedFung))]
    [InlineData(nameof(MechanicalModel.Schapery))]
    [InlineData(nameof(MechanicalModel.ModifiedSuperpositionMethod))]
    public void CreateCalculatorFacade_ByName_ReturnsValidFacade(string modelName)
    {
        IMechanicalModelCalculatorFacade facade = _factory.CreateCalculatorFacade(modelName);

        Assert.NotNull(facade);
    }

    [Fact]
    public void CreateCalculatorFacade_WithGenericInput_ForElastic_ReturnsConfiguredFacade()
    {
        GenericMechanicalModelInput input = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Elastic),
            MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
            ViscoelasticEffect = ViscoelasticEffect.Relaxation,
            ConstitutiveParameters = new ElasticConstitutiveParameters { YoungModulus = 100.0 },
            Strain = new MechanicalParameter(0.1)
        };

        IMechanicalModelCalculatorFacade facade = _factory.CreateCalculatorFacade(input);

        Assert.NotNull(facade);
    }

    [Fact]
    public void CreateCalculatorFacade_WithGenericInput_ForMaxwell_ReturnsConfiguredFacade()
    {
        GenericMechanicalModelInput input = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Maxwell),
            MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
            ViscoelasticEffect = ViscoelasticEffect.Relaxation,
            ConstitutiveParameters = new MaxwellConstitutiveParameters { Stiffness = 200.0, Viscosity = 50.0 },
            Strain = new MechanicalParameter(0.1)
        };

        IMechanicalModelCalculatorFacade facade = _factory.CreateCalculatorFacade(input);

        Assert.NotNull(facade);
    }
}
