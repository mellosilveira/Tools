using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Caching;

/// <summary>
/// Domain-specific cache for compiled reflection metadata required by mechanical model processing.
/// Wraps <see cref="Core.Caching.ISingleLevelCache"/> with typed, semantically named
/// methods that encapsulate key construction and compilation logic.
/// </summary>
public interface IMechanicalModelTypeCache
{
    /// <summary>
    /// Returns a compiled delegate that invokes the named method on the given calculator type.
    /// </summary>
    Func<object, object?[], object> GetOrAddMethodInvoker(Type calculatorType, string methodName);

    /// <summary>
    /// Returns the list of calculator methods applicable to the given relationship and viscoelastic effect,
    /// each pre-compiled into a <see cref="CalculatorMethodData"/>.
    /// </summary>
    CalculatorMethodData[] GetOrAddMethodDataList(Type calculatorType, MechanicalRelationship relationship, ViscoelasticEffect effect);

    /// <summary>
    /// Returns a compiled factory delegate that creates instances of the given output type.
    /// </summary>
    Func<MechanicalModelOutput> GetOrAddOutputFactory(Type outputType);

    /// <summary>
    /// Returns compiled property setter delegates for all settable properties of the given type.
    /// </summary>
    Dictionary<string, Action<object, object>> GetOrAddPropertySetters(Type type);

    /// <summary>
    /// Returns the constructor parameter types of the given type's single public constructor.
    /// </summary>
    Type[] GetOrAddConstructorParameterTypes(Type type);
}
