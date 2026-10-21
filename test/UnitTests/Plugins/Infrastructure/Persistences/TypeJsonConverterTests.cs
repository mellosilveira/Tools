using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using System.Text.Json;

namespace UnitTests.Plugins.Infrastructure.Persistences;

public sealed class TypeJsonConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new TypeJsonConverter() }
    };

    [Fact]
    public void Serialize_WritesAssemblyQualifiedName()
    {
        Type target = typeof(string);

        string json = JsonSerializer.Serialize(target, _options);

        Assert.Contains("System.String", json);
    }

    [Fact]
    public void Deserialize_FromValidTypeName_ResolvesType()
    {
        string json = JsonSerializer.Serialize(typeof(int), _options);

        Type? deserialized = JsonSerializer.Deserialize<Type>(json, _options);

        Assert.Equal(typeof(int), deserialized);
    }

    [Fact]
    public void Deserialize_FromUnknownTypeName_FallsBackToObject()
    {
        string json = "\"NonExistentNamespace.NonExistentType, NonExistentAssembly\"";

        Type? deserialized = JsonSerializer.Deserialize<Type>(json, _options);

        Assert.Equal(typeof(object), deserialized);
    }

    [Fact]
    public void Deserialize_FromEmptyOrWhitespace_ReturnsNull()
    {
        string json = "\"   \"";

        Type? deserialized = JsonSerializer.Deserialize<Type>(json, _options);

        Assert.Null(deserialized);
    }

    [Fact]
    public void SerializeAndDeserialize_DictionaryWithTypeKeys_RoundtripsSuccessfully()
    {
        Dictionary<Type, bool> dict = new()
        {
            [typeof(string)] = true,
            [typeof(int)] = false
        };

        string json = JsonSerializer.Serialize(dict, _options);
        Dictionary<Type, bool>? deserialized = JsonSerializer.Deserialize<Dictionary<Type, bool>>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Count);
        Assert.True(deserialized[typeof(string)]);
        Assert.False(deserialized[typeof(int)]);
    }
}
