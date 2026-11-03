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
    public CalculatorMethodData[] GetOrAddMethodDataList(Type calculatorType, LoadResponseRelationship relationship, ViscoelasticEffect effect)
        => cache.GetOrAdd($"MethodData:{calculatorType.FullName}:{relationship}:{effect}", () => BuildMethodDataList(calculatorType, relationship, effect));

    /// <inheritdoc/>
    public Func<MechanicalModelOutput> GetOrAddOutputFactory(Type outputType)
        => cache.GetOrAdd($"OutputFactory:{outputType.FullName}", () => CompileOutputFactory(outputType));

    /// <inheritdoc/>
    public Dictionary<string, Action<object, object>> GetOrAddPropertySetters(Type type)
        => cache.GetOrAdd($"PropertySetters:{type.FullName}", () => CompilePropertySetters(type));

    /// <inheritdoc/>
    public Type[] GetOrAddConstructorParameterTypes(Type type)
        => cache.GetOrAdd($"CtorParams:{type.FullName}", () => type.GetConstructors().Single().GetParameters().Select(p => p.ParameterType).ToArray());

    private static CalculatorMethodData[] BuildMethodDataList(Type calculatorType, LoadResponseRelationship relationship, ViscoelasticEffect effect)
        => [.. calculatorType.GetMethods()
            .Select(method => (Method: method, Attribute: method.GetCustomAttribute<MechanicalModelParameterCalculationAttribute>()))
            .Where(x => x.Attribute != null && x.Attribute.CanMethodBeInvoked(relationship, effect))
            .Select(x => new CalculatorMethodData(
                CompileMethodInvoker(x.Method),
                [.. x.Method.GetParameters().Select(p => p.Name)!],
                x.Attribute!.PropertyName))];

    /// <summary>
    /// Compiles a <see cref="MethodInfo"/> into a delegate, eliminating reflection overhead on every call.
    /// The compiled delegate is ~10-50x faster than <see cref="MethodBase.Invoke(object?, object?[])"/>.
    /// </summary>
    private static Func<object, object?[], object> CompileMethodInvoker(MethodInfo method)
    {
        ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
        ParameterExpression args = Expression.Parameter(typeof(object[]), "args");

        ParameterInfo[] parameters = method.GetParameters();
        Expression[] paramExpressions = new Expression[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            Type paramType = parameters[i].ParameterType;
            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(MechanicalModelInput<>))
            {
                Type constitutiveType = paramType.GetGenericArguments()[0];
                MethodInfo convertMethod = typeof(MechanicalModelTypeCache)
                    .GetMethod(nameof(ConvertMechanicalModelInput), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(constitutiveType);

                paramExpressions[i] = Expression.Call(
                    convertMethod,
                    Expression.ArrayIndex(args, Expression.Constant(i)));
            }
            else
            {
                paramExpressions[i] = Expression.Convert(
                    Expression.ArrayIndex(args, Expression.Constant(i)),
                    paramType);
            }
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
    /// Compiles a parameterless constructor into a factory delegate, replacing <see cref="Activator.CreateInstance(Type)"/>.
    /// </summary>
    private static Func<MechanicalModelOutput> CompileOutputFactory(Type outputType)
        => Expression.Lambda<Func<MechanicalModelOutput>>(Expression.New(outputType)).Compile();

    /// <summary>
    /// Compiles property setters for all settable properties of a type, replacing <see cref="PropertyInfo.SetValue(object?, object?)"/>.
    /// </summary>
    private static Dictionary<string, Action<object, object>> CompilePropertySetters(Type type)
        => type.GetProperties().Where(p => p.SetMethod != null).ToDictionary(p => p.Name, CompilePropertySetter);

    private static Action<object, object> CompilePropertySetter(PropertyInfo property)
    {
        ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
        ParameterExpression value = Expression.Parameter(typeof(object), "value");

        Expression body = Expression.Assign(
            Expression.Property(Expression.Convert(instance, property.DeclaringType!), property),
            Expression.Convert(value, property.PropertyType));

        return Expression.Lambda<Action<object, object>>(body, instance, value).Compile();
    }

    private static MechanicalModelInput<TConstitutiveParameters>? ConvertMechanicalModelInput<TConstitutiveParameters>(object? rawInput)
        where TConstitutiveParameters : ConstitutiveParameters
    {
        if (rawInput is null)
        {
            return null;
        }

        if (rawInput is MechanicalModelInput<TConstitutiveParameters> typedInput)
        {
            return typedInput;
        }

        if (rawInput is MechanicalModelInput<ConstitutiveParameters> genericInput)
        {
            if (genericInput.ConstitutiveParameters is TConstitutiveParameters typedParameters)
            {
                return new MechanicalModelInput<TConstitutiveParameters>(genericInput, typedParameters);
            }

            throw new InvalidCastException($"Cannot convert constitutive parameters of type '{genericInput.ConstitutiveParameters?.GetType().Name}' to expected type '{typeof(TConstitutiveParameters).Name}'.");
        }

        if (rawInput is MechanicalModelInput baseInput)
        {
            PropertyInfo? property = rawInput.GetType().GetProperty(nameof(MechanicalModelInput<ConstitutiveParameters>.ConstitutiveParameters));
            if (property?.GetValue(rawInput) is TConstitutiveParameters constitutive)
            {
                return new MechanicalModelInput<TConstitutiveParameters>(baseInput, constitutive);
            }
        }

        return (MechanicalModelInput<TConstitutiveParameters>)rawInput;
    }
}

/// <summary>
/// Holds a pre-compiled calculator method with its parameter names and output property target.
/// </summary>
public readonly record struct CalculatorMethodData(
    Func<object, object[], object> Invoker,
    string[] ParameterNames,
    string PropertyName);
