using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.MechanicsOfMaterials.Caching;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <inheritdoc cref="IMechanicalModelCalculatorFactory"/>
/// <param name="serviceLocator">The dynamic service locator to resolve keyed type resolvers and calculators.</param>
/// <param name="cache">The mechanical model type cache for compiled invokers and setters.</param>
public class MechanicalModelCalculatorFactory(ServiceLocator serviceLocator, IMechanicalModelTypeCache cache) : IMechanicalModelCalculatorFactory
{
    /// <inheritdoc/>
    public IMechanicalModelCalculatorFacade CreateCalculatorFacade(string mechanicalModelName)
    {
        IMechanicalModelTypeResolver typeResolver = serviceLocator.GetRequiredKeyedService<IMechanicalModelTypeResolver>(mechanicalModelName);
        object calculator = serviceLocator.GetService(typeResolver.Calculator)!;
        return new MechanicalModelCalculatorFacade(cache, calculator);
    }

    /// <inheritdoc/>
    public IMechanicalModelCalculatorFacade CreateCalculatorFacade(string mechanicalModelName, GenericMechanicalModelInput input)
    {
        IMechanicalModelTypeResolver typeResolver = serviceLocator.GetRequiredKeyedService<IMechanicalModelTypeResolver>(mechanicalModelName);
        object calculator = serviceLocator.GetService(typeResolver.Calculator)!;
        return new MechanicalModelCalculatorFacade(cache, typeResolver.Output, calculator, input);
    }

    /// <inheritdoc/>
    public IMechanicalModelCalculatorFacade CreateCalculatorFacade(GenericMechanicalModelInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return CreateCalculatorFacade(input.MechanicalModelName, input);
    }
}
