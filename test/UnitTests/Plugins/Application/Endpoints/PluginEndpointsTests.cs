using MelloSilveiraTools.Plugins.Application.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace UnitTests.Plugins.Application.Endpoints;

public sealed class PluginEndpointsTests
{
    [Fact]
    public void MapPluginEndpoints_WithDefaultPattern_RegistersRouteGroup()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        RouteGroupBuilder group = app.MapPluginEndpoints();

        Assert.NotNull(group);
    }

    [Fact]
    public void MapPluginEndpoints_WithCustomPattern_RegistersRouteGroup()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        RouteGroupBuilder group = app.MapPluginEndpoints("/custom/plugins");

        Assert.NotNull(group);
    }
}
