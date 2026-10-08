using MelloSilveiraTools.Core.ExtensionMethods;

namespace UnitTests;

public sealed class StringExtensionsTests
{
    // ── ToSnakeCase ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Id", "id")]
    [InlineData("Name", "name")]
    [InlineData("UserId", "user_id")]
    [InlineData("CreationTimestamp", "creation_timestamp")]
    [InlineData("StateAbbreviation", "state_abbreviation")]
    [InlineData("ProductId", "product_id")]
    [InlineData("name", "name")]           // already lower-case
    [InlineData("ABC", "a_b_c")]          // all caps
    public void ToSnakeCase_ConvertsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.ToSnakeCase());
    }

    [Fact]
    public void ToSnakeCase_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, string.Empty.ToSnakeCase());
    }

    // ── Remove ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Remove_SingleValue_RemovesIt()
    {
        Assert.Equal(string.Empty, "#WHERE".Remove("#WHERE"));
    }

    [Fact]
    public void Remove_ValueNotPresent_ReturnsOriginal()
    {
        Assert.Equal("SELECT 1", "SELECT 1".Remove("#WHERE"));
    }

    [Fact]
    public void Remove_MultipleArgs_AllAreRemovedSequentially()
    {
        // Regression test: the old code used `input.Replace` instead of `result.Replace`,
        // so only the last substitution had any effect.
        var result = "#WHERE #ORDERBY #LIMIT".Remove("#WHERE ", "#ORDERBY ");

        Assert.Equal("#LIMIT", result);
    }

    [Fact]
    public void Remove_MultipleArgs_CanRemoveAll()
    {
        var result = "abc".Remove("a", "b", "c");

        Assert.Equal(string.Empty, result);
    }

    // ── FromSnakeCaseToPascalCase / ToCamelCase ────────────────────────────────

    [Theory]
    [InlineData("user_id", "UserId")]
    [InlineData("creation_timestamp", "CreationTimestamp")]
    [InlineData("id", "Id")]
    public void FromSnakeCaseToPascalCase_ConvertsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.FromSnakeCaseToPascalCase());
    }

    [Theory]
    [InlineData("user_id", "userId")]
    [InlineData("creation_timestamp", "creationTimestamp")]
    [InlineData("id", "id")]
    public void FromSnakeCaseToCamelCase_ConvertsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.FromSnakeCaseToCamelCase());
    }

    // ── AddSpaceBeforeUpperCase ───────────────────────────────────────────────

    [Theory]
    [InlineData("HelloWorld", "Hello World")]
    [InlineData("camelCase", "camel Case")]
    [InlineData("ABC", "A B C")]
    [InlineData("single", "single")]
    public void AddSpaceBeforeUpperCase_InsertsSpacesCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.AddSpaceBeforeUpperCase());
    }
}
