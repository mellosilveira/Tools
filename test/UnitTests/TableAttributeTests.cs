using MelloSilveiraTools.Database.Infrastructure.Database.Attributes;

namespace UnitTests;

public sealed class TableAttributeTests
{
    // ── Explicit alias ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithExplicitAlias_UsesThatAlias()
    {
        var attr = new TableAttribute("product", "prd");

        Assert.Equal("product", attr.Name);
        Assert.Equal("prd",     attr.Alias);
    }

    // ── Auto-generated alias ───────────────────────────────────────────────────

    [Theory]
    [InlineData("product",              "product")]  // single word → full name
    [InlineData("order_item",           "oi")]       // two words → acronym
    [InlineData("user_managed_district","umd")]      // three words → acronym
    [InlineData("prajah_user",          "pu")]
    public void Constructor_AutoAlias_GeneratesCorrectAbbreviation(
        string tableName, string expectedAlias)
    {
        var attr = new TableAttribute(tableName);

        Assert.Equal(expectedAlias, attr.Alias);
    }

    // ── Integration: entity TableAttribute is read correctly ──────────────────

    [Fact]
    public void ProductEntity_HasExpectedTableNameAndAlias()
    {
        var attr = typeof(ProductEntity)
            .GetCustomAttributes(typeof(TableAttribute), inherit: false)
            .Cast<TableAttribute>()
            .Single();

        Assert.Equal("product", attr.Name);
        Assert.Equal("prd",     attr.Alias);
    }

    [Fact]
    public void OrderItemEntity_HasExpectedTableNameAndAlias()
    {
        var attr = typeof(OrderItemEntity)
            .GetCustomAttributes(typeof(TableAttribute), inherit: false)
            .Cast<TableAttribute>()
            .Single();

        Assert.Equal("order_item", attr.Name);
        Assert.Equal("ordi",       attr.Alias);
    }
}
