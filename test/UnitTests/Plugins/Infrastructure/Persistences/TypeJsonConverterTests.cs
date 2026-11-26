using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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

    [Fact]
    public void Deserialize_FromTypeWithStaleAssemblyQualifier_ResolvesViaLoadedAppDomain()
    {
        string json = $"\"{typeof(TypeJsonConverterTests).FullName}, WrongOrStaleAssemblyName\"";

        Type? deserialized = JsonSerializer.Deserialize<Type>(json, _options);

        Assert.Equal(typeof(TypeJsonConverterTests), deserialized);
    }

    [Fact]
    public void Deserialize_WithoutAssemblyComma_And_EmptyPropertyKey_FallbackToObject()
    {
        Assert.Equal(typeof(object), JsonSerializer.Deserialize<Type>("\"Unknown.SimpleTypeName\"", _options));

        Dictionary<Type, bool>? dict = JsonSerializer.Deserialize<Dictionary<Type, bool>>("{\"\":true}", _options);
        Assert.NotNull(dict);
        Assert.True(dict[typeof(object)]);
    }

    [Fact]
    public async Task Write_And_WriteAsPropertyName_WhenAssemblyQualifiedNameOrFullNameAreNull_UsesFallbacks()
    {
        TypeJsonConverter converter = new();

        Mock<Type> fullNameOnlyType = new();
        fullNameOnlyType.SetupGet(t => t.AssemblyQualifiedName).Returns((string?)null);
        fullNameOnlyType.SetupGet(t => t.FullName).Returns("Custom.FullNameOnly");
        fullNameOnlyType.SetupGet(t => t.Name).Returns("FullNameOnly");

        Mock<Type> shortNameOnlyType = new();
        shortNameOnlyType.SetupGet(t => t.AssemblyQualifiedName).Returns((string?)null);
        shortNameOnlyType.SetupGet(t => t.FullName).Returns((string?)null);
        shortNameOnlyType.SetupGet(t => t.Name).Returns("ShortNameOnly");

        using MemoryStream ms = new();
        using (Utf8JsonWriter writer = new(ms))
        {
            writer.WriteStartObject();
            converter.WriteAsPropertyName(writer, fullNameOnlyType.Object, _options);
            converter.Write(writer, fullNameOnlyType.Object, _options);
            converter.WriteAsPropertyName(writer, shortNameOnlyType.Object, _options);
            converter.Write(writer, shortNameOnlyType.Object, _options);
            writer.WriteEndObject();
        }

        string json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        Assert.Contains("\"Custom.FullNameOnly\":\"Custom.FullNameOnly\"", json);
        Assert.Contains("\"ShortNameOnly\":\"ShortNameOnly\"", json);

        RegisteredPlugin reg = new() { TypesLoadedStatus = null! };
        Assert.Empty(reg.TypesLoadedStatus);

        PluginValidator validator = new();
        Result nullPluginResult = await validator.ValidateAsync(null!);
        Assert.False(nullPluginResult.Success);

        PluginSettings settings = new()
        {
            StorageProvider = StorageProvider.Local,
            Directory = "plugins",
            DefaultCacheTarget = "file",
            PollInterval = TimeSpan.FromSeconds(30),
            PreviousVersionRetention = TimeSpan.FromHours(1),
            MaxPluginSizeBytes = 10L * 1024 * 1024,
            MaxPendingSubmissionsPerUser = 5
        };
        PluginFileProcessor fileProcessor = new(NullLogger<PluginFileProcessor>.Instance, Mock.Of<IFileStorageService>(), new PluginCache(new InMemoryTwoLevelCache()), settings);
        UploadPluginValidator uploadValidator = new(fileProcessor, new PluginSubmissionPersistence(NullLogger<PluginSubmissionPersistence>.Instance, Mock.Of<IFileStorageService>(), Mock.Of<IRepository>(), settings), settings);

        Mock<IFormFile> pathSepFile = new();
        pathSepFile.SetupGet(f => f.Length).Returns(10);
        pathSepFile.SetupGet(f => f.FileName).Returns("folder/Plugin.v1.0.0.dll");
        Result pathSepResult = await uploadValidator.ValidateAsync(new UploadPluginRequest { File = pathSepFile.Object });
        Assert.False(pathSepResult.Success);

        Mock<IFormFile> dotDotFile = new();
        dotDotFile.SetupGet(f => f.Length).Returns(10);
        dotDotFile.SetupGet(f => f.FileName).Returns("Plugin..v1.0.0.dll");
        Result dotDotResult = await uploadValidator.ValidateAsync(new UploadPluginRequest { File = dotDotFile.Object });
        Assert.False(dotDotResult.Success);
    }
}
