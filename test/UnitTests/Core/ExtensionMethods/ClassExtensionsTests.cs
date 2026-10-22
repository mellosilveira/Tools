using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using Npgsql;
using System.Reflection;
using UnitTests.Database;

namespace UnitTests.Core.ExtensionMethods;

public sealed class ClassExtensionsTests
{
    // ── BuildParameters ────────────────────────────────────────────────────────

    [Fact]
    public void BuildParameters_Default_IncludesBaseClassProperties()
    {
        ProductEntity entity = new() { Id = 1, Name = "Widget", Price = 50 };

        List<NpgsqlParameter> parameters = entity.BuildParameters().ToList();

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
        ProductEntity entity = new() { Id = 1, Name = "Widget", Price = 50 };

        List<NpgsqlParameter> parameters = entity.BuildParameters(useDeclaredProperties: true).ToList();

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

        List<NpgsqlParameter> parameters = entity.BuildParameters().ToList();

        Assert.Empty(parameters);
    }

    [Fact]
    public void BuildParameters_ValuesMatchEntityProperties()
    {
        ProductEntity entity = new() { Id = 99, Name = "Test", Price = 200 };

        List<NpgsqlParameter> parameters = entity.BuildParameters().ToList();

        Assert.Equal(99L, parameters.First(p => p.ParameterName == "Id").Value);
        Assert.Equal("Test", parameters.First(p => p.ParameterName == "Name").Value);
        Assert.Equal(200L, parameters.First(p => p.ParameterName == "Price").Value);
    }

    // ── BuildParametersFromCollection ──────────────────────────────────────────

    [Fact]
    public void BuildParametersFromCollection_UseOneBased_NotZeroBased_Suffixes()
    {
        ProductEntity[] entities =
        [
            new ProductEntity { Id = 1, Name = "First",  Price = 10 },
            new ProductEntity { Id = 2, Name = "Second", Price = 20 },
        ];

        List<NpgsqlParameter> parameters = entities.BuildParametersFromCollection().ToList();

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
        ProductEntity[] entities =
        [
            new ProductEntity { Id = 1, Name = "A", Price = 1 },
            new ProductEntity { Id = 2, Name = "B", Price = 2 },
            new ProductEntity { Id = 3, Name = "C", Price = 3 },
        ];

        List<NpgsqlParameter> parameters = entities.BuildParametersFromCollection().ToList();

        Assert.Equal(12, parameters.Count);
        Assert.Contains(parameters, p => p.ParameterName == "Name_3");
    }

    [Fact]
    public void BuildParametersFromCollection_ValuesMatchEntities()
    {
        ProductEntity[] entities =
        [
            new ProductEntity { Id = 42, Name = "Alpha", Price = 99 }
        ];

        List<NpgsqlParameter> parameters = entities.BuildParametersFromCollection().ToList();

        Assert.Equal(42L, parameters.First(p => p.ParameterName == "Id_1").Value);
        Assert.Equal("Alpha", parameters.First(p => p.ParameterName == "Name_1").Value);
        Assert.Equal(99L, parameters.First(p => p.ParameterName == "Price_1").Value);
    }

    [Fact]
    public void BuildParametersFromCollection_EmptyCollection_ReturnsEmpty()
    {
        List<NpgsqlParameter> parameters = Array.Empty<ProductEntity>()
            .BuildParametersFromCollection()
            .ToList();

        Assert.Empty(parameters);
    }

    // ── GetValuesInHierarchy ───────────────────────────────────────────────────

    [Fact]
    public void GetValuesInHierarchy_ReturnsValuesFromAllLevels()
    {
        ProductEntity entity = new() { Id = 7, Name = "X", Price = 3 };

        List<object?> values = entity.GetValuesInHierarchy().ToList();

        // Expect: Id, CreationTimestamp, Name, Price (4 values, in hierarchy order)
        Assert.Equal(4, values.Count);
        Assert.Contains(7L, values);
        Assert.Contains("X", values);
    }

    // ── ToTask & GetValues & GetPropertyNamesAndValuesInHierarchy ───────────────

    [Fact]
    public async Task ToTask_WrapsInstanceInCompletedTask()
    {
        ProductEntity entity = new() { Id = 10, Name = "Completed", Price = 50 };

        Task<ProductEntity> task = entity.ToTask();

        Assert.True(task.IsCompletedSuccessfully);
        ProductEntity awaited = await task;
        Assert.Same(entity, awaited);
    }

    [Fact]
    public void GetValues_WithExplicitProperties_ExtractsValues()
    {
        ProductEntity entity = new() { Id = 100, Name = "Explicit", Price = 250 };
        PropertyInfo[] properties =
        [
            typeof(ProductEntity).GetProperty(nameof(ProductEntity.Name))!,
            typeof(ProductEntity).GetProperty(nameof(ProductEntity.Price))!
        ];

        List<object?> values = entity.GetValues(properties).ToList();

        Assert.Equal(2, values.Count);
        Assert.Equal("Explicit", values[0]);
        Assert.Equal(250L, values[1]);
    }

    [Fact]
    public void GetPropertyNamesAndValuesInHierarchy_FiltersByAttribute()
    {
        CategoryEntity category = new() { Id = 5, Code = "CAT01", Description = "Electronics" };

        Dictionary<string, object?>? dict = category.GetPropertyNamesAndValuesInHierarchy<CategoryEntity, UniqueColumnAttribute>();

        Assert.NotNull(dict);
        Assert.True(dict.ContainsKey("Code"));
        Assert.Equal("CAT01", dict["Code"]);
        Assert.False(dict.ContainsKey("Description"));
    }

    // ── GetValueAsIntIfEnum ────────────────────────────────────────────────────

    private enum TestSampleEnum
    {
        First = 10,
        Second = 20
    }

    private sealed class SampleObject
    {
        public TestSampleEnum Status { get; set; }
        public string? Text { get; set; }
        public int Number { get; set; }
    }

    [Fact]
    public void GetValueAsIntIfEnum_WhenPropertyIsEnum_ReturnsInteger()
    {
        SampleObject obj = new() { Status = TestSampleEnum.Second };
        PropertyInfo prop = typeof(SampleObject).GetProperty(nameof(SampleObject.Status))!;

        object? result = prop.GetCustomValue(obj);

        Assert.Equal(20, result);
    }

    [Fact]
    public void GetValueAsIntIfEnum_WhenPropertyIsNotEnum_ReturnsOriginalValue()
    {
        SampleObject obj = new() { Text = "hello", Number = 42 };
        PropertyInfo textProp = typeof(SampleObject).GetProperty(nameof(SampleObject.Text))!;
        PropertyInfo numProp = typeof(SampleObject).GetProperty(nameof(SampleObject.Number))!;

        object? textResult = textProp.GetCustomValue(obj);
        object? numResult = numProp.GetCustomValue(obj);

        Assert.Equal("hello", textResult);
        Assert.Equal(42, numResult);
    }

    [Fact]
    public void GetValueAsIntIfEnum_WhenPropertyValueIsNull_ReturnsNull()
    {
        SampleObject obj = new() { Text = null };
        PropertyInfo textProp = typeof(SampleObject).GetProperty(nameof(SampleObject.Text))!;

        object? result = textProp.GetCustomValue(obj);

        Assert.Null(result);
    }
}
