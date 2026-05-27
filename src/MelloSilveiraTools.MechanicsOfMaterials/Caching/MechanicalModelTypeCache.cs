using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using System.Linq.Expressions;
using System.Reflection;

namespace MelloSilveiraTools.MechanicsOfMaterials.Caching;

/// <inheritdoc cref="IMechanicalModelTypeCache"/>
public class MechanicalModelTypeCache(ISingleLevelCache cache) : IMechanicalModelTypeCache
{
    /// <inheritdoc/>
    public Func<object, object?[], object> GetOrAddMethodInvoker(Type calculatorType, string methodName)
        => cache.GetOrAdd($"Invoker:{calculatorType.FullName}:{methodName}", () => CompileMethodInvoker(calculatorType.GetMethod(methodName)!));

    /// <inheritdoc/>
    public CalculatorMethodData[] GetOrAddMethodDataList(Type calculatorType, MechanicalRelationship relationship, ViscoelasticEffect effect)
        => cache.GetOrAdd($"MethodData:{calculatorType.FullName}:{relationship}:{effect}", () => BuildMethodDataList(calculatorType, relationship, effect));

    /// <inheritdoc/>
    public Func<MechanicalModelOutput> GetOrAddOutputFactory(Type outputType)
        => cache.GetOrAdd($"OutputFactory:{outputType.FullName}", () => CompileOutputFactory(outputType));

    /// <inheritdoc/>
    public Dictionary<string, Action<object, object>> GetOrAddPropertySetters(Type type)
        => cache.GetOrAdd($"OutputFactory:{type.FullName}", () => CompilePropertySetters(type));

    /// <inheritdoc/>
    public Type[] GetOrAddConstructorParameterTypes(Type type)
        => cache.GetOrAdd($"CtorParams:{type.FullName}", () => type.GetConstructors().Single().GetParameters().Select(p => p.ParameterType).ToArray());

    private static CalculatorMethodData[] BuildMethodDataList(Type calculatorType, MechanicalRelationship relationship, ViscoelasticEffect effect) 
        => [.. calculatorType.GetMethods()
            .Select(method => (Method: method, Attribute: method.GetCustomAttribute<MechanicalModelParameterCalculationAttribute>()))
            .Where(x => x.Attribute != null && x.Attribute.CanMethodBeInvoked(relationship, effect))
            .Select(x => new CalculatorMethodData(
                CompileMethodInvoker(x.Method),
                [.. x.Method.GetParameters().Select(p => p.Name)!],
                x.Attribute!.PropertyName))];

    /// <summary>
    /// Compiles a <see cref="MethodInfo"/> into a delegate, eliminating reflection overhead on every call.
    /// The compiled delegate is ~10-50x faster than <see cref="MethodBase.Invoke"/>.
    /// </summary>
    private static Func<object, object?[], object> CompileMethodInvoker(MethodInfo method)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var args = Expression.Parameter(typeof(object[]), "args");

        ParameterInfo[] parameters = method.GetParameters();
        var paramExpressions = new Expression[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            paramExpressions[i] = Expression.Convert(
                Expression.ArrayIndex(args, Expression.Constant(i)),
                parameters[i].ParameterType);
        }

        Expression call = Expression.Call(
            Expression.Convert(instance, method.DeclaringType!),
            method,
            paramExpressions);

        Expression body = method.ReturnType == typeof(void)
            ? Expression.Block(call, Expression.Constant(null, typeof(object)))
            : Expression.Convert(call, typeof(object));

        return Expression.Lambda<Func<object, object?[], object>>(body, instance, args).Compile();
    }

    /// <summary>
    /// Compiles a parameterless constructor into a factory delegate, replacing <see cref="Activator.CreateInstance"/>.
    /// </summary>
    private static Func<MechanicalModelOutput> CompileOutputFactory(Type outputType)
        => Expression.Lambda<Func<MechanicalModelOutput>>(Expression.New(outputType)).Compile();

    /// <summary>
    /// Compiles property setters for all settable properties of a type, replacing <see cref="PropertyInfo.SetValue"/>.
    /// </summary>
    private static Dictionary<string, Action<object, object>> CompilePropertySetters(Type type) 
        => type.GetProperties().Where(p => p.SetMethod != null).ToDictionary(p => p.Name, CompilePropertySetter);

    private static Action<object, object> CompilePropertySetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");

        Expression body = Expression.Assign(
            Expression.Property(Expression.Convert(instance, property.DeclaringType!), property),
            Expression.Convert(value, property.PropertyType));

        return Expression.Lambda<Action<object, object>>(body, instance, value).Compile();
    }
}

/// <summary>
/// Holds a pre-compiled calculator method with its parameter names and output property target.
/// </summary>
public readonly record struct CalculatorMethodData(
    Func<object, object[], object> Invoker,
    string[] ParameterNames,
    string PropertyName);
