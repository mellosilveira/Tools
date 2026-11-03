using MelloSilveiraTools.Plugins.Infrastructure.Models;
using System.Text.Json;

namespace UnitTests.Plugins.Infrastructure.Models;

public sealed class PluginVersionJsonConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new PluginVersionJsonConverter() }
    };

    [Fact]
    public void Serialize_WritesStringRepresentation()
    {
        PluginVersion version = new(1, 2, 3);

        string json = JsonSerializer.Serialize(version, _options);

        Assert.Equal("\"v1.2.3\"", json);
    }

    [Fact]
    public void Deserialize_FromString_ParsesVersionCorrectly()
    {
        string json = "\"v2.4.6\"";

        PluginVersion version = JsonSerializer.Deserialize<PluginVersion>(json, _options);

        Assert.Equal(new PluginVersion(2, 4, 6), version);
    }

    [Fact]
    public void Deserialize_FromEmptyString_ReturnsDefault()
    {
        string json = "\"\"";

        PluginVersion version = JsonSerializer.Deserialize<PluginVersion>(json, _options);

        Assert.Equal(PluginVersion.Default, version);
    }

    [Fact]
    public void Deserialize_FromObjectWithNameProperty_ParsesVersionCorrectly()
    {
        string json = "{\"Name\":\"v3.1.4\"}";

        PluginVersion version = JsonSerializer.Deserialize<PluginVersion>(json, _options);

        Assert.Equal(new PluginVersion(3, 1, 4), version);
    }

    [Fact]
    public void Deserialize_FromObjectWithNumericFields_ParsesVersionCorrectly()
    {
        string json = "{\"Major\":5,\"Minor\":10,\"Patch\":15}";

        PluginVersion version = JsonSerializer.Deserialize<PluginVersion>(json, _options);

        Assert.Equal(new PluginVersion(5, 10, 15), version);
    }

    [Fact]
    public void Deserialize_FromOtherJsonToken_ReturnsDefault()
    {
        string json = "123";

        PluginVersion version = JsonSerializer.Deserialize<PluginVersion>(json, _options);

        Assert.Equal(PluginVersion.Default, version);
    }
}
