namespace UnitTests.Core.Providers;

using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using Microsoft.Extensions.DependencyInjection;
using Moq;

public class ServiceLocatorTests
{
    public interface ITestService
    {
        string GetMessage();
    }

    public class TestService : ITestService
    {
        public string GetMessage() => "Hello";
    }

    public class OtherService
    {
    }

    [Fact]
    public void DynamicServiceProvider_GetServiceGeneric_ResolvesRegisteredService()
    {
        IServiceCollection staticServices = new ServiceCollection();
        ServiceProvider staticProvider = staticServices.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(staticProvider);

        TestService testInstance = new();
        dynamicProvider.AddKeyed("testKey", testInstance);
        dynamicProvider.Add(typeof(ITestService), typeof(TestService));

        ITestService? resolved = dynamicProvider.GetService<ITestService>();

        Assert.NotNull(resolved);
        Assert.Equal("Hello", resolved.GetMessage());
    }

    [Fact]
    public void DynamicServiceProvider_GetServiceGeneric_ReturnsNullWhenNotRegistered()
    {
        IServiceCollection staticServices = new ServiceCollection();
        ServiceProvider staticProvider = staticServices.BuildServiceProvider();
        InMemoryDynamicServiceProvider dynamicProvider = new(staticProvider);

        ITestService? resolved = dynamicProvider.GetService<ITestService>();

        Assert.Null(resolved);
    }

    [Fact]
    public void ServiceLocator_GetRequiredServiceGeneric_ResolvesFromDynamicProviderFirst()
    {
        IServiceCollection staticServices = new ServiceCollection();
        TestService staticService = new();
        staticServices.AddSingleton<ITestService>(staticService);
        ServiceProvider staticProvider = staticServices.BuildServiceProvider();

        Mock<IDynamicServiceProvider> dynamicProviderMock = new();
        TestService dynamicService = new();
        dynamicProviderMock.Setup(d => d.GetService<ITestService>()).Returns(dynamicService);

        ServiceLocator locator = new(dynamicProviderMock.Object, staticProvider);

        ITestService resolved = locator.GetRequiredService<ITestService>();

        Assert.Same(dynamicService, resolved);
    }

    [Fact]
    public void ServiceLocator_GetRequiredServiceGeneric_FallsBackToStaticProvider()
    {
        IServiceCollection staticServices = new ServiceCollection();
        TestService staticService = new();
        staticServices.AddSingleton<ITestService>(staticService);
        ServiceProvider staticProvider = staticServices.BuildServiceProvider();

        Mock<IDynamicServiceProvider> dynamicProviderMock = new();
        dynamicProviderMock.Setup(d => d.GetService<ITestService>()).Returns((ITestService?)null);

        ServiceLocator locator = new(dynamicProviderMock.Object, staticProvider);

        ITestService resolved = locator.GetRequiredService<ITestService>();

        Assert.Same(staticService, resolved);
    }

    [Fact]
    public void ServiceLocator_GetRequiredServiceGeneric_ThrowsWhenNotFoundInEitherProvider()
    {
        IServiceCollection staticServices = new ServiceCollection();
        ServiceProvider staticProvider = staticServices.BuildServiceProvider();

        Mock<IDynamicServiceProvider> dynamicProviderMock = new();
        dynamicProviderMock.Setup(d => d.GetService<ITestService>()).Returns((ITestService?)null);

        ServiceLocator locator = new(dynamicProviderMock.Object, staticProvider);

        Assert.Throws<InvalidOperationException>(() => locator.GetRequiredService<ITestService>());
    }

    [Fact]
    public void ServiceLocator_GetServiceGeneric_ReturnsNullWhenNotFoundInEitherProvider()
    {
        IServiceCollection staticServices = new ServiceCollection();
        ServiceProvider staticProvider = staticServices.BuildServiceProvider();

        Mock<IDynamicServiceProvider> dynamicProviderMock = new();
        dynamicProviderMock.Setup(d => d.GetService<ITestService>()).Returns((ITestService?)null);

        ServiceLocator locator = new(dynamicProviderMock.Object, staticProvider);

        ITestService? resolved = locator.GetService<ITestService>();

        Assert.Null(resolved);
    }

    [Fact]
    public void ServiceLocator_NonGenericAndKeyedOverloads_ResolveDynamicFallbackAndNullOrThrow()
    {
        TestService staticService = new();
        TestService keyedStaticService = new();
        IServiceCollection staticServices = new ServiceCollection();
        staticServices.AddSingleton<ITestService>(staticService);
        staticServices.AddKeyedSingleton<ITestService>("k-static", keyedStaticService);
        ServiceProvider staticProvider = staticServices.BuildServiceProvider();

        TestService dynamicService = new();
        TestService keyedDynamicService = new();
        Mock<IDynamicServiceProvider> dynamicProviderMock = new();
        dynamicProviderMock.Setup(d => d.GetService(typeof(ITestService))).Returns(dynamicService);
        dynamicProviderMock.Setup(d => d.GetService(typeof(OtherService))).Returns((object?)null);
        dynamicProviderMock.Setup(d => d.GetKeyedService<ITestService>("k-dynamic")).Returns(keyedDynamicService);
        dynamicProviderMock.Setup(d => d.GetKeyedService<ITestService>("k-static")).Returns((ITestService?)null);
        dynamicProviderMock.Setup(d => d.GetKeyedService<ITestService>("k-missing")).Returns((ITestService?)null);

        ServiceLocator locator = new(dynamicProviderMock.Object, staticProvider);

        Assert.Same(dynamicService, locator.GetRequiredService(typeof(ITestService)));
        Assert.Same(dynamicService, locator.GetService(typeof(ITestService)));
        Assert.Null(locator.GetService(typeof(OtherService)));
        Assert.Throws<InvalidOperationException>(() => locator.GetRequiredService(typeof(OtherService)));

        Assert.Same(keyedDynamicService, locator.GetRequiredKeyedService<ITestService>("k-dynamic"));
        Assert.Same(keyedStaticService, locator.GetRequiredKeyedService<ITestService>("k-static"));
        Assert.Throws<InvalidOperationException>(() => locator.GetRequiredKeyedService<ITestService>("k-missing"));

        Assert.Same(keyedDynamicService, locator.GetKeyedService<ITestService>("k-dynamic"));
        Assert.Same(keyedStaticService, locator.GetKeyedService<ITestService>("k-static"));
        Assert.Null(locator.GetKeyedService<ITestService>("k-missing"));
    }
}
