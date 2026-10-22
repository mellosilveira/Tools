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
}
