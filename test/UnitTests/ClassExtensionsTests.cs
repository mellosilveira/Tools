using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Database.Models.Entities;

namespace UnitTests;

public sealed class ClassExtensionsTests
{
    // ── BuildParameters ────────────────────────────────────────────────────────

    [Fact]
    public void BuildParameters_Default_IncludesBaseClassProperties()
    {
        var entity = new ProductEntity { Id = 1, Name = "Widget", Price = 50 };

        var parameters = entity.BuildParameters().ToList();

        // EntityBase: Id, CreationTimestamp; ProductEntity: Name, Price → 4 total
        Assert.Equal(4, parameters.Count);
        Assert.Contains(parameters, p => p.ParameterName == "Id");
        Assert.Contains(parameters, p => p.ParameterName == "CreationTimestamp");
        Assert.Contains(parameters, p => p.ParameterName == "Name");
        Assert.Contains(parameters, p => p.ParameterName == "Price");
    }

    [Fact]
    public void BuildParameters_UseDeclaredPropertiesTrue_ExcludesBaseClassProperties()
    {
        var entity = new ProductEntity { Id = 1, Name = "Widget", Price = 50 };

        var parameters = entity.BuildParameters(useDeclaredProperties: true).ToList();

        // Only properties declared directly on ProductEntity
        Assert.Equal(2, parameters.Count);
        Assert.Contains(parameters, p => p.ParameterName == "Name");
        Assert.Contains(parameters, p => p.ParameterName == "Price");
        Assert.DoesNotContain(parameters, p => p.ParameterName == "Id");
        Assert.DoesNotContain(parameters, p => p.ParameterName == "CreationTimestamp");
    }

    [Fact]
    public void BuildParameters_NullObject_ReturnsEmpty()
    {
        ProductEntity? entity = null;

        var parameters = entity.BuildParameters().ToList();

        Assert.Empty(parameters);
    }

    [Fact]
    public void BuildParameters_ValuesMatchEntityProperties()
    {
        var entity = new ProductEntity { Id = 99, Name = "Test", Price = 200 };

        var parameters = entity.BuildParameters().ToList();

        Assert.Equal(99L,    parameters.First(p => p.ParameterName == "Id").Value);
        Assert.Equal("Test", parameters.First(p => p.ParameterName == "Name").Value);
        Assert.Equal(200L,   parameters.First(p => p.ParameterName == "Price").Value);
    }

    // ── BuildParametersFromCollection ──────────────────────────────────────────

    [Fact]
    public void BuildParametersFromCollection_UseOneBased_NotZeroBased_Suffixes()
    {
        var entities = new[]
        {
            new ProductEntity { Id = 1, Name = "First",  Price = 10 },
            new ProductEntity { Id = 2, Name = "Second", Price = 20 },
        };

        var parameters = entities.BuildParametersFromCollection().ToList();

        // Must be 1-based (_1, _2) to match CreateBatchInsertSql placeholders
        Assert.Contains(parameters, p => p.ParameterName == "Id_1");
        Assert.Contains(parameters, p => p.ParameterName == "Name_1");
        Assert.Contains(parameters, p => p.ParameterName == "Id_2");
        Assert.Contains(parameters, p => p.ParameterName == "Name_2");
        Assert.DoesNotContain(parameters, p => p.ParameterName == "Id_0");
    }

    [Fact]
    public void BuildParametersFromCollection_ParameterCount_IsPropsTimesRows()
    {
        // ProductEntity has 4 column-attributed properties; 3 entities → 12 params
        var entities = new[]
        {
            new ProductEntity { Id = 1, Name = "A", Price = 1 },
            new ProductEntity { Id = 2, Name = "B", Price = 2 },
            new ProductEntity { Id = 3, Name = "C", Price = 3 },
        };

        var parameters = entities.BuildParametersFromCollection().ToList();

        Assert.Equal(12, parameters.Count);
        Assert.Contains(parameters, p => p.ParameterName == "Name_3");
    }

    [Fact]
    public void BuildParametersFromCollection_ValuesMatchEntities()
    {
        var entities = new[]
        {
            new ProductEntity { Id = 42, Name = "Alpha", Price = 99 }
        };

        var parameters = entities.BuildParametersFromCollection().ToList();

        Assert.Equal(42L,     parameters.First(p => p.ParameterName == "Id_1").Value);
        Assert.Equal("Alpha", parameters.First(p => p.ParameterName == "Name_1").Value);
        Assert.Equal(99L,     parameters.First(p => p.ParameterName == "Price_1").Value);
    }

    [Fact]
    public void BuildParametersFromCollection_EmptyCollection_ReturnsEmpty()
    {
        var parameters = Array.Empty<ProductEntity>()
            .BuildParametersFromCollection()
            .ToList();

        Assert.Empty(parameters);
    }

    // ── GetValuesInHierarchy ───────────────────────────────────────────────────

    [Fact]
    public void GetValuesInHierarchy_ReturnsValuesFromAllLevels()
    {
        var entity = new ProductEntity { Id = 7, Name = "X", Price = 3 };

        var values = entity.GetValuesInHierarchy().ToList();

        // Expect: Id, CreationTimestamp, Name, Price (4 values, in hierarchy order)
        Assert.Equal(4, values.Count);
        Assert.Contains(7L,  values);
        Assert.Contains("X", values);
    }
}
