using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace UnitTests.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;

public class MechanicalModelTypeResolversTests
{
    public static TheoryData<IMechanicalModelTypeResolver, string, Type, Type, Type> ResolversData() => new()
    {
        {
            new ElasticModelTypeResolver(),
            nameof(MechanicalModel.Elastic),
            typeof(ElasticConstitutiveParameters),
            typeof(ElasticModelOutput),
            typeof(IElasticModelCalculator)
        },
        {
            new FungModelTypeResolver(),
            nameof(MechanicalModel.Fung),
            typeof(FungConstitutiveParameters),
            typeof(QuasiLinearModelOutput),
            typeof(IFungModelCalculator)
        },
        {
            new MaxwellModelTypeResolver(),
            nameof(MechanicalModel.Maxwell),
            typeof(MaxwellConstitutiveParameters),
            typeof(MaxwellModelOutput),
            typeof(IMaxwellModelCalculator)
        },
        {
            new ModifiedSuperpositionMethodTypeResolver(),
            nameof(MechanicalModel.ModifiedSuperpositionMethod),
            typeof(ModifiedSuperpositionMethodConstitutiveParameters),
            typeof(ModifiedSuperpositionMethodOutput),
            typeof(IModifiedSuperpositionMethodCalculator)
        },
        {
            new SchaperyModelTypeResolver(),
            nameof(MechanicalModel.Schapery),
            typeof(SchaperyConstitutiveParameters),
            typeof(SchaperyModelOutput),
            typeof(ISchaperyModelCalculator)
        },
        {
            new SimplifiedFungModelTypeResolver(),
            nameof(MechanicalModel.SimplifiedFung),
            typeof(SimplifiedFungConstitutiveParameters),
            typeof(QuasiLinearModelOutput),
            typeof(ISimplifiedFungModelCalculator)
        }
    };

    [Theory]
    [MemberData(nameof(ResolversData))]
    public void Resolvers_ShouldCorrectlyMapTypesAndInheritance(
        IMechanicalModelTypeResolver resolver,
        string expectedName,
        Type expectedParamsType,
        Type expectedOutputType,
        Type expectedCalculatorType)
    {
        Assert.Equal(expectedName, resolver.MechanicalModelName);
        Assert.Equal(expectedParamsType, resolver.ConstitutiveParameters);
        Assert.Equal(expectedOutputType, resolver.Output);
        Assert.Equal(expectedCalculatorType, resolver.Calculator);

        Assert.True(typeof(ConstitutiveParameters).IsAssignableFrom(resolver.ConstitutiveParameters));
        Assert.True(typeof(MechanicalModelOutput).IsAssignableFrom(resolver.Output));
    }
}
