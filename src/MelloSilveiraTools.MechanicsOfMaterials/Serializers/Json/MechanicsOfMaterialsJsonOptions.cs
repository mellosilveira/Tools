using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MelloSilveiraTools.MechanicsOfMaterials.Serializers.Json;

/// <summary>
/// Centralized JSON serialization, schema-export, and example-generation options for <c>MelloSilveiraTools.MechanicsOfMaterials</c>.
/// </summary>
internal static class MechanicsOfMaterialsJsonOptions
{
    /// <summary>
    /// Shared web-default JSON serialization options configured with case-insensitive property matching,
    /// <see cref="DefaultJsonTypeInfoResolver"/>, and <see cref="JsonStringEnumConverter"/>.
    /// </summary>
    internal static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Exports the JSON Schema representation of <paramref name="type"/> using <see cref="Default"/> options.
    /// </summary>
    internal static JsonNode GetJsonSchemaAsNode(Type type) => JsonSchemaExporter.GetJsonSchemaAsNode(Default, type);

    /// <summary>
    /// Builds a human-readable example JSON payload for <paramref name="type"/> using <see cref="Default"/> options.
    /// Uses realistic constitutive presets for built-in parameter models and falls back to metadata-driven
    /// example generation for outputs and dynamically loaded plugin types.
    /// </summary>
    /// <param name="type">Target constitutive parameter or output model type.</param>
    /// <returns>A <see cref="JsonNode"/> containing a representative JSON example.</returns>
    internal static JsonNode CreateExampleJsonNode(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        object? builtInExample = CreateBuiltInExampleInstance(type);
        if (builtInExample is not null)
        {
            JsonNode? serialized = JsonSerializer.SerializeToNode(builtInExample, type, Default);
            if (serialized is not null)
                return serialized;
        }

        HashSet<Type> visited = [];
        return BuildExampleNode(type, propertyName: null, visited);
    }

    /// <summary>
    /// Deserializes a <see cref="JsonElement"/> into the specified <paramref name="returnType"/> using <see cref="Default"/> options.
    /// </summary>
    internal static T? SafeDeserialize<T>(ILogger logger, JsonElement element, Type returnType)
    {
        try
        {
            return (T?)element.Deserialize(returnType, Default);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize JSON element into type. {@ReturnType}.", returnType);
            return default;
        }
    }

    /// <summary>
    /// Deserializes a <see cref="JsonElement"/> into the specified <paramref name="returnType"/> using <see cref="Default"/> options.
    /// </summary>
    internal static object? Deserialize(JsonElement element, Type returnType) => element.Deserialize(returnType, Default);

    /// <summary>
    /// Deserializes a <see cref="JsonElement"/> into the specified <typeparamref name="T"/> using <see cref="Default"/> options.
    /// </summary>
    internal static object? Deserialize<T>(JsonElement element) => element.Deserialize<T>(Default);

    private static object? CreateBuiltInExampleInstance(Type type)
    {
        if (type == typeof(ElasticConstitutiveParameters))
            return new ElasticConstitutiveParameters { YoungModulus = 100.0 };

        if (type == typeof(MaxwellConstitutiveParameters))
            return new MaxwellConstitutiveParameters { Viscosity = 500.0, Stiffness = 100.0 };

        if (type == typeof(FungConstitutiveParameters))
        {
            return new FungConstitutiveParameters(
                elasticStressConstant: 10.0,
                elasticPowerConstant: 15.0,
                new ReducedRelaxationFunction(RelaxationStiffness: 0.1, FastRelaxationTime: 0.01, SlowRelaxationTime: 100.0));
        }

        if (type == typeof(SimplifiedFungConstitutiveParameters))
        {
            return new SimplifiedFungConstitutiveParameters(
                elasticStressConstant: 10.0,
                elasticPowerConstant: 15.0,
                new PronySeries(0.5, [0.5, -0.1], 0.0, 100.0));
        }

        if (type == typeof(SchaperyConstitutiveParameters))
        {
            return new SchaperyConstitutiveParameters
            {
                G0 = 1.0,
                G1 = 1.0,
                G2 = 1.0,
                J0 = 0.01,
                Ge = 10.0,
                He = new ConstantFunction(1.0, 0.0, 100.0),
                H1 = new ConstantFunction(1.0, 0.0, 100.0),
                H2 = new ConstantFunction(1.0, 0.0, 100.0),
                TransientRelaxationFunction = new PowerLaw([1.0, 0.1], 0.0, 100.0),
                TransientCreepCompliance = new PronySeries(0.5, [0.5, -0.1], 0.0, 100.0)
            };
        }

        if (type == typeof(ModifiedSuperpositionMethodConstitutiveParameters))
        {
            return new ModifiedSuperpositionMethodConstitutiveParameters
            {
                InitialYoungModulus = new ConstantFunction(100.0, 0.0, 100.0),
                StressRelaxationRate = new PolynomialFunction([0.05, 0.01], 0.0, 100.0)
            };
        }

        return null;
    }

    private static JsonNode BuildExampleNode(Type rawType, string? propertyName, HashSet<Type> visited)
    {
        Type type = Nullable.GetUnderlyingType(rawType) ?? rawType;

        if (type == typeof(string))
            return JsonValue.Create("example")!;

        if (type == typeof(bool))
            return JsonValue.Create(true)!;

        if (type.IsEnum)
        {
            string[] names = Enum.GetNames(type);
            string first = names.Length > 0 ? names[0] : "Default";
            return JsonValue.Create(first)!;
        }

        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            return JsonValue.Create(GetNumericExample(propertyName))!;

        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
            || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte))
            return JsonValue.Create(1)!;

        if (type == typeof(Guid))
            return JsonValue.Create(Guid.Empty)!;

        if (type == typeof(DateTimeOffset) || type == typeof(DateTime))
            return JsonValue.Create("2026-01-01T00:00:00Z")!;

        if (type == typeof(TimeSpan))
            return JsonValue.Create("00:00:01")!;

        if (type == typeof(Function))
        {
            ConstantFunction sampleFunction = new(1.0, 0.0, 100.0);
            JsonNode? functionNode = JsonSerializer.SerializeToNode(sampleFunction, Default);
            if (functionNode is not null)
                return functionNode;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            JsonArray array = [];
            Type? elementType = type.IsArray
                ? type.GetElementType()
                : type.GetInterfaces()
                    .Concat([type])
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    ?.GetGenericArguments()[0];

            if (elementType is not null && !visited.Contains(elementType))
                array.Add(BuildExampleNode(elementType, propertyName, visited));

            return array;
        }

        if (!visited.Add(type))
            return new JsonObject();

        try
        {
            JsonObject obj = new();
            JsonTypeInfo typeInfo = Default.GetTypeInfo(type);
            foreach (JsonPropertyInfo property in typeInfo.Properties)
            {
                if (property.Get is null && property.Set is null)
                    continue;

                if (property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
                {
                    obj[property.Name] = JsonValue.Create(GetNumericExample(property.Name));
                    continue;
                }

                obj[property.Name] = BuildExampleNode(property.PropertyType, property.Name, visited);
            }

            return obj;
        }
        finally
        {
            visited.Remove(type);
        }
    }

    private static double GetNumericExample(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return 1.0;

        if (propertyName.Contains("strain", StringComparison.OrdinalIgnoreCase))
            return 0.05;

        if (propertyName.Contains("stress", StringComparison.OrdinalIgnoreCase))
            return 5.0;

        if (propertyName.Contains("time", StringComparison.OrdinalIgnoreCase))
            return 0.5;

        if (propertyName.Contains("modulus", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("stiffness", StringComparison.OrdinalIgnoreCase))
            return 100.0;

        if (propertyName.Contains("viscosity", StringComparison.OrdinalIgnoreCase))
            return 500.0;

        return 1.0;
    }
}
