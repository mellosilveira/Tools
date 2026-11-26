using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands;
using MelloSilveiraTools.Plugins.Application.Commands.Get;
using MelloSilveiraTools.Plugins.Application.Commands.Reload;
using MelloSilveiraTools.Plugins.Application.Validators;

namespace UnitTests.Plugins.Application.Validators;

public sealed class PluginValidatorTests
{
    private readonly PluginValidator _defaultValidator = new();
    private readonly PluginValidator<GetPluginsRequest> _getValidator = new();
    private readonly PluginValidator<ReloadPluginsRequest> _reloadValidator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("v1.0.0")]
    [InlineData("2.1.3")]
    public async Task Validate_PluginsRequest_WhenVersionIsOmittedOrValid_ReturnsSuccess(string? version)
    {
        PluginsRequest request = new() { Version = version };

        Result result = await _defaultValidator.ValidateAsync(request);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("v1.0")]
    [InlineData("v1.a.2")]
    public async Task Validate_PluginsRequest_WhenVersionIsInvalid_ReturnsBadRequest(string version)
    {
        PluginsRequest request = new() { Version = version };

        Result result = await _defaultValidator.ValidateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        Assert.Contains("Invalid plugin version.", result.Messages);
    }

    [Fact]
    public async Task Validate_GetPluginsRequest_ValidatesCorrectly()
    {
        GetPluginsRequest validRequest = new() { Name = "Contoso", Version = "v1.2.3", FullyLoaded = true };
        GetPluginsRequest invalidRequest = new() { Name = "Contoso", Version = "wrong", FullyLoaded = true };

        Result validResult = await _getValidator.ValidateAsync(validRequest);
        Result invalidResult = await _getValidator.ValidateAsync(invalidRequest);

        Assert.True(validResult.Success);
        Assert.False(invalidResult.Success);
    }

    [Fact]
    public async Task Validate_ReloadPluginsRequest_ValidatesCorrectly()
    {
        ReloadPluginsRequest validRequest = new() { Name = "Contoso", Version = "v1.0.0", Force = true };
        ReloadPluginsRequest invalidRequest = new() { Name = "Contoso", Version = "invalid-version", Force = true };

        Result validResult = await _reloadValidator.ValidateAsync(validRequest);
        Result invalidResult = await _reloadValidator.ValidateAsync(invalidRequest);

        Assert.True(validResult.Success);
        Assert.False(invalidResult.Success);
    }
}
