using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Caching;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <summary>
/// Unit tests for <see cref="MechanicalModelCalculatorFacade"/>, <see cref="MechanicalModelCalculatorFactory"/>,
/// and <see cref="MechanicalModelTypeCache"/>.
/// </summary>
public sealed class MechanicalModelCalculatorFacadeAndCacheTests
{
    private static ServiceProvider BuildProvider()
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
        services.AddMechanicsOfMaterialsServices(addMechanicalModels: true);
        return services.BuildServiceProvider();
    }

    private sealed record CustomDerivedMechanicalModelInput : MechanicalModelInput
    {
        public required ElasticConstitutiveParameters ConstitutiveParameters { get; init; }
    }

    private sealed class DummyVoidTarget
    {
        public int Calls { get; private set; }
        public DummyVoidTarget(int seed) => Calls = seed;
        public void Ping(string _) => Calls++;
    }

    [Fact]
    public void Facade_ExecutesCalculateAndDirectMethodsAcrossAllRouteCombinations()
    {
        using ServiceProvider provider = BuildProvider();
        IMechanicalModelCalculatorFactory factory = provider.GetRequiredService<IMechanicalModelCalculatorFactory>();

        SpecimenParameter specimen = new(false, false, 0.0, 0.1, 0.0001, Vector3D.Empty);
        ElasticConstitutiveParameters elasticParams = new() { YoungModulus = 200.0 };

        // 1. Direct methods via name-only facade
        IMechanicalModelCalculatorFacade directFacade = factory.CreateCalculatorFacade(nameof(MechanicalModel.Elastic));
        GenericMechanicalModelInput directInput = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Elastic),
            ConstitutiveParameters = elasticParams,
            Specimen = specimen,
            Strain = new MechanicalParameter(0.1),
            Stress = new MechanicalParameter(20.0),
            Displacement = new MechanicalParameter(0.01),
            Force = new MechanicalParameter(2000.0)
        };

        Assert.Equal(20.0, directFacade.CalculateStress(directInput, 0.0, 0.1), 6);
        Assert.Equal(0.1, directFacade.CalculateStrain(directInput, 0.0, 20.0), 6);
        Assert.Equal(2000.0, directFacade.CalculateForce(directInput, 0.0, 0.01), 6);
        Assert.Equal(0.01, directFacade.CalculateDisplacement(directInput, 0.0, 2000.0), 6);

        // 2. ForceDisplacement + Relaxation using CreateCalculatorFacade(resolver, input)
        GenericMechanicalModelInput fdRelaxInput = directInput with
        {
            LoadResponseRelationship = LoadResponseRelationship.ForceDisplacement,
            ViscoelasticEffect = ViscoelasticEffect.Relaxation
        };
        IMechanicalModelCalculatorFacade fdRelaxFacade = factory.CreateCalculatorFacade(new ElasticModelTypeResolver(), fdRelaxInput);
        ElasticModelOutput fdRelaxOut = Assert.IsType<ElasticModelOutput>(fdRelaxFacade.Calculate(0.0));
        Assert.Equal(0.01, fdRelaxOut.Displacement!.Value, 6);

        // 3. ForceDisplacement + Creep
        GenericMechanicalModelInput fdCreepInput = directInput with
        {
            LoadResponseRelationship = LoadResponseRelationship.ForceDisplacement,
            ViscoelasticEffect = ViscoelasticEffect.Creep
        };
        IMechanicalModelCalculatorFacade fdCreepFacade = factory.CreateCalculatorFacade(fdCreepInput);
        ElasticModelOutput fdCreepOut = Assert.IsType<ElasticModelOutput>(fdCreepFacade.Calculate(0.0));
        Assert.Equal(2000.0, fdCreepOut.Force!.Value, 6);

        // 4. StressStrain + Relaxation (Maxwell to exercise ParameterCalculation + attribute filtering)
        GenericMechanicalModelInput ssRelaxInput = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Maxwell),
            LoadResponseRelationship = LoadResponseRelationship.StressStrain,
            ViscoelasticEffect = ViscoelasticEffect.Relaxation,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            ConstitutiveParameters = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 200.0 },
            Strain = new MechanicalParameter(0.1, new PolynomialFunction([0.0, 0.01]))
        };
        IMechanicalModelCalculatorFacade ssRelaxFacade = factory.CreateCalculatorFacade(ssRelaxInput);
        MaxwellModelOutput maxwellOut = Assert.IsType<MaxwellModelOutput>(ssRelaxFacade.Calculate(1.0));
        Assert.Equal(2.0, maxwellOut.RelaxationTime!.Value, 6);

        // 5. StressStrain + Creep
        GenericMechanicalModelInput ssCreepInput = directInput with
        {
            LoadResponseRelationship = LoadResponseRelationship.StressStrain,
            ViscoelasticEffect = ViscoelasticEffect.Creep
        };
        IMechanicalModelCalculatorFacade ssCreepFacade = factory.CreateCalculatorFacade(ssCreepInput);
        ElasticModelOutput ssCreepOut = Assert.IsType<ElasticModelOutput>(ssCreepFacade.Calculate(0.0));
        Assert.Equal(20.0, ssCreepOut.Stress!.Value, 6);

        // 6. Invalid combination & null input
        IMechanicalModelTypeCache cache = provider.GetRequiredService<IMechanicalModelTypeCache>();
        Assert.Throws<ArgumentNullException>(() => new MechanicalModelCalculatorFacade(
            cache,
            typeof(ElasticModelOutput),
            new MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity.ElasticModelCalculator(),
            null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateCalculatorFacade(directInput with
        {
            LoadResponseRelationship = (LoadResponseRelationship)999
        }));
    }

    [Fact]
    public void MechanicalModelTypeCache_CoversConstructorParamsVoidMethodsAndConversionErrors()
    {
        using ServiceProvider provider = BuildProvider();
        MechanicalModelTypeCache cache = new(provider.GetRequiredService<ISingleLevelCache>());

        Assert.Equal([typeof(int)], cache.GetOrAddConstructorParameterTypes(typeof(DummyVoidTarget)));

        DummyVoidTarget target = new(5);
        Func<object, object?[], object> voidInvoker = cache.GetOrAddMethodInvoker(typeof(DummyVoidTarget), nameof(DummyVoidTarget.Ping));
        object result = voidInvoker(target, ["hi"]);
        Assert.Null(result);
        Assert.Equal(6, target.Calls);

        // Mismatched ConstitutiveParameters inside GenericMechanicalModelInput triggers InvalidCastException
        Func<object, object?[], object> stressInvoker = cache.GetOrAddMethodInvoker(
            typeof(MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity.ElasticModelCalculator),
            nameof(IMechanicalModelCalculator<>.CalculateStress));

        GenericMechanicalModelInput mismatched = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Maxwell),
            ConstitutiveParameters = new MaxwellConstitutiveParameters { Stiffness = 10, Viscosity = 20 },
            Strain = new MechanicalParameter(0.1)
        };
        Assert.Throws<InvalidCastException>(() => stressInvoker(
            new MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity.ElasticModelCalculator(),
            [mismatched, 0.0, 0.1]));

        // Passing null MechanicalModelInput returns null (hits ConvertMechanicalModelInput null guard before deref)
        Assert.Throws<NullReferenceException>(() => stressInvoker(
            new MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity.ElasticModelCalculator(),
            [null, 0.0, 0.1]));

        // Passing custom MechanicalModelInput subclass (non-generic baseInput branch) and invalid cast branches
        CustomDerivedMechanicalModelInput customDerived = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Elastic),
            ConstitutiveParameters = new ElasticConstitutiveParameters { YoungModulus = 200.0 },
            Strain = new MechanicalParameter(0.1)
        };
        double customStress = Assert.IsType<double>(stressInvoker(
            new MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity.ElasticModelCalculator(),
            [customDerived, 0.0, 0.1]));
        Assert.Equal(20.0, customStress, 6);

        Assert.Throws<InvalidCastException>(() => stressInvoker(
            new MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity.ElasticModelCalculator(),
            ["not-an-input", 0.0, 0.1]));
    }
}
