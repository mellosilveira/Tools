using MelloSilveiraTools.Core.Providers.Dynamics;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Core.Providers.Dynamics;

/// <summary>
/// Unit tests for <see cref="InMemoryDynamicServiceProvider"/> verifying dynamic service registration,
/// lazy factory activation, keyed instance storage, and typed resolution.
/// </summary>
public sealed class InMemoryDynamicServiceProviderTests
{
    private sealed class SampleService(string name)
    {
        public string Name => name;
    }

    private interface ITestContract
    {
        int Value { get; }
    }

    private sealed class TestImplementation : ITestContract
    {
        public int Value => 42;
    }

    /// <summary>
    /// Verifies that dynamic types can be registered and lazily resolved with custom constructor parameters.
    /// </summary>
    [Fact]
    public void Add_And_GetService_ResolvesLazyInstanceWithParameters()
    {
        // Arrange
        ServiceCollection services = new();
        ServiceProvider rootProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(rootProvider);

        // Act
        dynamicProvider.Add(typeof(SampleService), typeof(SampleService), "DynamicallyCreated");
        SampleService? resolved = dynamicProvider.GetService<SampleService>();

        // Assert
        Assert.NotNull(resolved);
        Assert.Equal("DynamicallyCreated", resolved.Name);
    }

    /// <summary>
    /// Verifies that interface mappings can be added and resolved via generic GetService.
    /// </summary>
    [Fact]
    public void Add_InterfaceMapping_ResolvesGenericType()
    {
        // Arrange
        ServiceCollection services = new();
        ServiceProvider rootProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(rootProvider);

        // Act
        dynamicProvider.Add(typeof(ITestContract), typeof(TestImplementation));
        ITestContract? resolved = dynamicProvider.GetService<ITestContract>();

        // Assert
        Assert.NotNull(resolved);
        Assert.Equal(42, resolved.Value);
    }

    /// <summary>
    /// Verifies that keyed instances can be stored and retrieved by key.
    /// </summary>
    [Fact]
    public void AddKeyed_And_GetKeyedService_ResolvesByKey()
    {
        // Arrange
        ServiceCollection services = new();
        ServiceProvider rootProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(rootProvider);
        SampleService instance = new("KeyedInstance");

        // Act
        dynamicProvider.AddKeyed("my-special-key", instance);
        SampleService? resolved = dynamicProvider.GetKeyedService<SampleService>("my-special-key");
        SampleService? missing = dynamicProvider.GetKeyedService<SampleService>("non-existent-key");

        // Assert
        Assert.NotNull(resolved);
        Assert.Same(instance, resolved);
        Assert.Null(missing);
    }

    /// <summary>
    /// Verifies that resolving an unregistered type returns null.
    /// </summary>
    [Fact]
    public void GetService_UnregisteredType_ReturnsNull()
    {
        // Arrange
        ServiceCollection services = new();
        ServiceProvider rootProvider = services.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(rootProvider);

        // Act
        SampleService? resolved = dynamicProvider.GetService<SampleService>();

        // Assert
        Assert.Null(resolved);
    }
}
