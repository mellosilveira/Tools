using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.Infrastructure.Database.Attributes;
using NpgsqlTypes;

namespace UnitTests;

public sealed class TypeExtensionsTests
{
    // ── GetDbTypeFromPropertyType ──────────────────────────────────────────────

    [Theory]
    [InlineData(typeof(string),         NpgsqlDbType.Text)]
    [InlineData(typeof(bool),           NpgsqlDbType.Boolean)]
    [InlineData(typeof(bool?),          NpgsqlDbType.Boolean)]
    [InlineData(typeof(short),          NpgsqlDbType.Smallint)]
    [InlineData(typeof(short?),         NpgsqlDbType.Smallint)]
    [InlineData(typeof(int),            NpgsqlDbType.Integer)]
    [InlineData(typeof(int?),           NpgsqlDbType.Integer)]
    [InlineData(typeof(long),           NpgsqlDbType.Bigint)]
    [InlineData(typeof(long?),          NpgsqlDbType.Bigint)]
    [InlineData(typeof(float),          NpgsqlDbType.Real)]
    [InlineData(typeof(float?),         NpgsqlDbType.Real)]
    [InlineData(typeof(double),         NpgsqlDbType.Double)]
    [InlineData(typeof(double?),        NpgsqlDbType.Double)]
    [InlineData(typeof(decimal),        NpgsqlDbType.Numeric)]
    [InlineData(typeof(decimal?),       NpgsqlDbType.Numeric)]
    [InlineData(typeof(byte[]),         NpgsqlDbType.Bytea)]
    [InlineData(typeof(DateTime),       NpgsqlDbType.Timestamp)]
    [InlineData(typeof(DateTime?),      NpgsqlDbType.Timestamp)]
    [InlineData(typeof(DateTimeOffset), NpgsqlDbType.TimestampTz)]
    [InlineData(typeof(DateTimeOffset?),NpgsqlDbType.TimestampTz)]
    public void GetDbTypeFromPropertyType_KnownType_ReturnsCorrectNpgsqlDbType(
        Type type, NpgsqlDbType expected)
    {
        Assert.Equal(expected, type.GetDbTypeFromPropertyType());
    }

    [Fact]
    public void GetDbTypeFromPropertyType_StringArray_ReturnsTextArray()
    {
        // NpgsqlDbType.Text | NpgsqlDbType.Array cannot be used as a constant in InlineData.
        Assert.Equal(NpgsqlDbType.Text | NpgsqlDbType.Array,
            typeof(string[]).GetDbTypeFromPropertyType());
    }

    [Fact]
    public void GetDbTypeFromPropertyType_UnsupportedType_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => typeof(Guid).GetDbTypeFromPropertyType());
    }

    // ── GetPropertiesInHierarchy ───────────────────────────────────────────────

    [Fact]
    public void GetPropertiesInHierarchy_IncludesBaseAndDerivedProperties()
    {
        var names = typeof(ProductEntity)
            .GetPropertiesInHierarchy()
            .Select(p => p.Name)
            .ToArray();

        Assert.Contains("Id",                names); // EntityBase
        Assert.Contains("CreationTimestamp", names); // EntityBase
        Assert.Contains("Name",              names); // ProductEntity
        Assert.Contains("Price",             names); // ProductEntity
    }

    [Fact]
    public void GetPropertiesInHierarchy_BaseClassPropertiesComeBefore_DerivedOnes()
    {
        var names = typeof(ProductEntity)
            .GetPropertiesInHierarchy()
            .Select(p => p.Name)
            .ToArray();

        Assert.True(
            Array.IndexOf(names, "Id") < Array.IndexOf(names, "Name"),
            "Base class property 'Id' should appear before derived class property 'Name'.");
    }

    [Fact]
    public void GetPropertiesInHierarchyWithAttribute_FiltersToAttributedPropertiesOnly()
    {
        var names = typeof(CategoryEntity)
            .GetPropertiesInHierarchy<UniqueColumnAttribute>()
            .Select(p => p.Name)
            .ToArray();

        Assert.Contains("Code",        names);
        Assert.DoesNotContain("Description", names);
        Assert.DoesNotContain("Id",          names);
        Assert.DoesNotContain("CreationTimestamp", names);
    }

    // ── GetDeclaredProperties ─────────────────────────────────────────────────

    [Fact]
    public void GetDeclaredProperties_ReturnsOnlyPropertiesDeclaredOnType()
    {
        var names = typeof(ProductEntity)
            .GetDeclaredProperties()
            .Select(p => p.Name)
            .ToArray();

        Assert.Contains("Name",  names);
        Assert.Contains("Price", names);
        Assert.DoesNotContain("Id",                names);
        Assert.DoesNotContain("CreationTimestamp", names);
    }
}
