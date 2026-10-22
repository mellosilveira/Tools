using MelloSilveiraTools.Plugins.Infrastructure.Models;

namespace UnitTests.Plugins.Infrastructure.Models;

public sealed class PluginVersionTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        PluginVersion version = new(1, 2, 3);

        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Patch);
        Assert.Equal("v1.2.3", version.Name);
        Assert.Equal("v1.2.3", version.ToString());
    }

    [Fact]
    public void Default_ReturnsZeroVersion()
    {
        PluginVersion defaultVersion = PluginVersion.Default;

        Assert.Equal(0, defaultVersion.Major);
        Assert.Equal(0, defaultVersion.Minor);
        Assert.Equal(0, defaultVersion.Patch);
        Assert.Equal("v0.0.0", defaultVersion.Name);
    }

    [Fact]
    public void CompareTo_ComparesMajorMinorPatchCorrectly()
    {
        PluginVersion v100 = new(1, 0, 0);
        PluginVersion v200 = new(2, 0, 0);
        PluginVersion v110 = new(1, 1, 0);
        PluginVersion v111 = new(1, 1, 1);
        PluginVersion v100Dup = new(1, 0, 0);

        Assert.True(v100.CompareTo(v200) < 0);
        Assert.True(v200.CompareTo(v100) > 0);
        Assert.True(v100.CompareTo(v110) < 0);
        Assert.True(v110.CompareTo(v111) < 0);
        Assert.Equal(0, v100.CompareTo(v100Dup));
    }

    [Fact]
    public void RelationalOperators_WorkAsExpected()
    {
        PluginVersion v1 = new(1, 0, 0);
        PluginVersion v2 = new(2, 0, 0);
        PluginVersion v1Same = new(1, 0, 0);

        Assert.True(v1 < v2);
        Assert.False(v2 < v1);
        Assert.True(v2 > v1);
        Assert.False(v1 > v2);
        Assert.True(v1 <= v2);
        Assert.True(v1 <= v1Same);
        Assert.True(v2 >= v1);
        Assert.True(v1 >= v1Same);
    }

    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("v10.20.30", 10, 20, 30)]
    [InlineData("v1.0.0_LoadedPlugin", 1, 0, 0)]
    [InlineData("v2.1.0_RegisteredPlugin", 2, 1, 0)]
    public void Parse_ValidString_ReturnsExpectedVersion(string input, int expectedMajor, int expectedMinor, int expectedPatch)
    {
        PluginVersion version = PluginVersion.Parse(input);

        Assert.Equal(expectedMajor, version.Major);
        Assert.Equal(expectedMinor, version.Minor);
        Assert.Equal(expectedPatch, version.Patch);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("v1.2")]
    [InlineData("v1.a.2")]
    [InlineData("")]
    public void Parse_InvalidString_ThrowsException(string input)
    {
        Assert.ThrowsAny<Exception>(() => PluginVersion.Parse(input));
    }

    [Theory]
    [InlineData("v1.2.3", true, 1, 2, 3)]
    [InlineData("2.0.1", true, 2, 0, 1)]
    [InlineData("invalid", false, 0, 0, 0)]
    [InlineData(null, false, 0, 0, 0)]
    [InlineData("", false, 0, 0, 0)]
    [InlineData("   ", false, 0, 0, 0)]
    public void TryParse_EvaluatesCorrectly(string? input, bool expectedSuccess, int expectedMajor, int expectedMinor, int expectedPatch)
    {
        bool success = PluginVersion.TryParse(input, out PluginVersion parsed);

        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expectedMajor, parsed.Major);
        Assert.Equal(expectedMinor, parsed.Minor);
        Assert.Equal(expectedPatch, parsed.Patch);
    }

    [Fact]
    public void SafeParse_ReturnsNullOnInvalidOrMalformed()
    {
        PluginVersion? valid = PluginVersion.SafeParse("v1.2.3");
        PluginVersion? invalid = PluginVersion.SafeParse("invalid");
        PluginVersion? nullInput = PluginVersion.SafeParse(null);

        Assert.NotNull(valid);
        Assert.Equal(new PluginVersion(1, 2, 3), valid.Value);
        Assert.Null(invalid);
        Assert.Null(nullInput);
    }
}
