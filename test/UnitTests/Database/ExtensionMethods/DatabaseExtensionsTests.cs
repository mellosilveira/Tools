using Dapper;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using Npgsql;
using NpgsqlTypes;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace UnitTests.Database.ExtensionMethods;

public sealed class DatabaseExtensionsTests
{
    private enum SampleEnum
    {
        First = 1,
        Second = 2
    }

    private sealed record EnumAndNullableEntity
    {
        [PrimaryKeyColumn]
        public long Id { get; init; }

        [Column]
        public SampleEnum Mode { get; init; }

        [Column]
        public string? Notes { get; init; }

        public string IgnoredNonColumn { get; init; } = "ignored";
    }

    [Filter(typeof(OrderItemEntity))]
    private sealed record OrderItemSearchFilter : FilterBase
    {
        [FilterColumn(FilterClause.Equal)]
        public int? Quantity { get; init; }

        [FilterColumn(FilterClause.Like, "Name", "product")]
        public string? ProductNamePattern { get; init; }

        [FilterColumn(FilterClause.ILike, null, "product")]
        public string? Name { get; init; }

        [FilterColumn(FilterClause.Equal)]
        public SampleEnum? Status { get; init; }
    }

    private sealed record UnattributedFilter : FilterBase
    {
        public string? Anything { get; init; }
    }

    private sealed class SetValuesTarget
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public int ReadOnlyProp { get; } = 7;
    }

    private sealed class FakeDbDataReader(string[] columns, object[] values) : DbDataReader
    {
        private int _row = -1;

        public override int FieldCount => columns.Length;
        public override bool HasRows => true;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override int Depth => 0;
        public override object this[int ordinal] => values[ordinal];
        public override object this[string name] => values[GetOrdinal(name)];
        public override bool Read() => ++_row == 0;
        public override string GetName(int ordinal) => columns[ordinal];
        public override int GetOrdinal(string name) => Array.FindIndex(columns, c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
        public override object GetValue(int ordinal) => values[ordinal];
        public override bool IsDBNull(int ordinal) => values[ordinal] is DBNull;
        public override bool GetBoolean(int ordinal) => (bool)values[ordinal];
        public override byte GetByte(int ordinal) => (byte)values[ordinal];
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => (char)values[ordinal];
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
        public override string GetDataTypeName(int ordinal) => values[ordinal].GetType().Name;
        public override DateTime GetDateTime(int ordinal) => (DateTime)values[ordinal];
        public override decimal GetDecimal(int ordinal) => (decimal)values[ordinal];
        public override double GetDouble(int ordinal) => (double)values[ordinal];
        public override IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
        public override Type GetFieldType(int ordinal) => values[ordinal].GetType();
        public override float GetFloat(int ordinal) => (float)values[ordinal];
        public override Guid GetGuid(int ordinal) => (Guid)values[ordinal];
        public override short GetInt16(int ordinal) => (short)values[ordinal];
        public override int GetInt32(int ordinal) => (int)values[ordinal];
        public override long GetInt64(int ordinal) => (long)values[ordinal];
        public override string GetString(int ordinal) => (string)values[ordinal];
        public override int GetValues(object[] target) { values.CopyTo(target, 0); return values.Length; }
        public override bool NextResult() => false;
    }

    public static TheoryData<Type, NpgsqlDbType> SupportedDbTypeMappings => new()
    {
        { typeof(SampleEnum), NpgsqlDbType.Integer },
        { typeof(SampleEnum?), NpgsqlDbType.Integer },
        { typeof(string), NpgsqlDbType.Text },
        { typeof(bool), NpgsqlDbType.Boolean },
        { typeof(bool?), NpgsqlDbType.Boolean },
        { typeof(short), NpgsqlDbType.Smallint },
        { typeof(short?), NpgsqlDbType.Smallint },
        { typeof(int), NpgsqlDbType.Integer },
        { typeof(int?), NpgsqlDbType.Integer },
        { typeof(long), NpgsqlDbType.Bigint },
        { typeof(long?), NpgsqlDbType.Bigint },
        { typeof(float), NpgsqlDbType.Real },
        { typeof(float?), NpgsqlDbType.Real },
        { typeof(double), NpgsqlDbType.Double },
        { typeof(double?), NpgsqlDbType.Double },
        { typeof(decimal), NpgsqlDbType.Numeric },
        { typeof(decimal?), NpgsqlDbType.Numeric },
        { typeof(byte[]), NpgsqlDbType.Bytea },
        { typeof(string[]), NpgsqlDbType.Text | NpgsqlDbType.Array },
        { typeof(DateTime), NpgsqlDbType.Timestamp },
        { typeof(DateTime?), NpgsqlDbType.Timestamp },
        { typeof(DateTimeOffset), NpgsqlDbType.TimestampTz },
        { typeof(DateTimeOffset?), NpgsqlDbType.TimestampTz },
        { typeof(IList), NpgsqlDbType.Array },
        { typeof(IEnumerable), NpgsqlDbType.Array },
        { typeof(IEnumerator), NpgsqlDbType.Array }
    };

    [Theory]
    [MemberData(nameof(SupportedDbTypeMappings))]
    public void GetDbTypeFromPropertyType_MapsAllSupportedTypes(Type clrType, NpgsqlDbType expected)
    {
        Assert.Equal(expected, clrType.GetDbTypeFromPropertyType());
    }

    [Fact]
    public void GetDbTypeFromPropertyType_ThrowsForUnsupportedType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => typeof(Guid).GetDbTypeFromPropertyType());
    }

    [Fact]
    public void SortOrder_ToNpgsqlString_HandlesAscDescAndInvalid()
    {
        Assert.Equal("ORDER BY 1 ASC", SortOrder.Asc.ToNpgsqlString());
        Assert.Equal("ORDER BY 1 DESC", SortOrder.Desc.ToNpgsqlString());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((SortOrder)999).ToNpgsqlString());
    }

    [Fact]
    public void ClassExtensions_BuildParametersAndFromCollection_HandleNullsEnumsAndDeclaredFlags()
    {
        EnumAndNullableEntity? nullEntity = null;
        Assert.Empty(nullEntity.BuildParameters().ToList());

        EnumAndNullableEntity entity = new() { Id = 10, Mode = SampleEnum.Second, Notes = null };
        List<NpgsqlParameter> allParams = [.. entity.BuildParameters(useDeclaredProperties: false)];
        Assert.Equal(3, allParams.Count);
        Assert.Equal((int)SampleEnum.Second, allParams.Single(p => p.ParameterName == "Mode").Value);
        Assert.Equal(DBNull.Value, allParams.Single(p => p.ParameterName == "Notes").Value);

        // Inherited property exclusion when useDeclaredProperties is true
        ProductEntity product = new() { Id = 5, CreationTimestamp = DateTimeOffset.UtcNow, Name = "ACL", Price = 100 };
        List<NpgsqlParameter> declaredOnly = [.. product.BuildParameters(useDeclaredProperties: true)];
        List<NpgsqlParameter> withInherited = [.. product.BuildParameters(useDeclaredProperties: false)];
        Assert.Equal(["Name", "Price"], declaredOnly.Select(p => p.ParameterName).ToArray());
        Assert.Equal(4, withInherited.Count);

        // Collection parameters
        ICollection<EnumAndNullableEntity> emptyList = [];
        Assert.Empty(emptyList.BuildParametersFromCollection().ToList());

        ICollection<EnumAndNullableEntity> batch =
        [
            new() { Id = 1, Mode = SampleEnum.First, Notes = "a" },
            new() { Id = 2, Mode = SampleEnum.Second, Notes = null }
        ];
        List<NpgsqlParameter> batchParams = [.. batch.BuildParametersFromCollection()];
        Assert.Equal(6, batchParams.Count);
        Assert.Equal(1L, batchParams.Single(p => p.ParameterName == "Id_1").Value);
        Assert.Equal((int)SampleEnum.Second, batchParams.Single(p => p.ParameterName == "Mode_2").Value);
        Assert.Equal(DBNull.Value, batchParams.Single(p => p.ParameterName == "Notes_2").Value);
    }

    [Fact]
    public void ClassExtensions_BuildWhereClauses_CoverNullsMissingAttrEmptyJoinedAndPatterns()
    {
        OrderItemSearchFilter? nullFilter = null;
        Assert.Equal((null, null), nullFilter.BuildWhereClauseAndParameters());
        (string? nullNpgSql, List<NpgsqlParameter> nullNpgParams) = nullFilter.BuildWhereClauseAndNpgsqlParameters();
        Assert.Null(nullNpgSql);
        Assert.Empty(nullNpgParams);

        UnattributedFilter noAttr = new() { Anything = "x" };
        Assert.Equal((null, null), noAttr.BuildWhereClauseAndParameters());

        OrderItemSearchFilter blankFilter = new() { Quantity = null, ProductNamePattern = "   ", Name = null };
        Assert.Equal((null, null), blankFilter.BuildWhereClauseAndParameters());

        OrderItemSearchFilter active = new()
        {
            Quantity = 3,
            ProductNamePattern = "lig",
            Name = "knee",
            Status = SampleEnum.First
        };

        (string? whereDapper, DynamicParameters? dapperParams) = active.BuildWhereClauseAndParameters();
        Assert.NotNull(whereDapper);
        Assert.NotNull(dapperParams);
        Assert.Contains("WHERE ordi.quantity = @Quantity", whereDapper);
        Assert.Contains("prd.name LIKE @ProductNamePattern", whereDapper);
        Assert.Contains("prd.name ILIKE @Name", whereDapper);
        Assert.Contains("ordi.status = @Status", whereDapper);
        Assert.Equal("%lig%", dapperParams.Get<string>("ProductNamePattern"));
        Assert.Equal("%knee%", dapperParams.Get<string>("Name"));
        Assert.Equal((int)SampleEnum.First, dapperParams.Get<int>("Status"));

        (string? whereNpgsql, List<NpgsqlParameter> npgsqlParams) = active.BuildWhereClauseAndNpgsqlParameters();
        Assert.Equal(whereDapper, whereNpgsql);
        Assert.Equal(4, npgsqlParams.Count);
        Assert.Equal("%lig%", npgsqlParams.Single(p => p.ParameterName == "ProductNamePattern").Value);
        Assert.Equal(NpgsqlDbType.Integer, npgsqlParams.Single(p => p.ParameterName == "Status").NpgsqlDbType);
    }

    [Fact]
    public void ClassExtensions_SetValuesAndGetCustomValue_WorkAsExpected()
    {
        SetValuesTarget target = new();
        Dictionary<string, string> dict = new()
        {
            ["Title"] = "Hello",
            ["Missing"] = "Ignored"
        };

        target.SetValues(typeof(SetValuesTarget), dict);
        Assert.Equal("Hello", target.Title);

        PropertyInfo notesProp = typeof(EnumAndNullableEntity).GetProperty(nameof(EnumAndNullableEntity.Notes))!;
        EnumAndNullableEntity sample = new() { Id = 1, Mode = SampleEnum.First, Notes = "ok" };
        Assert.Equal("ok", notesProp.GetCustomValue(sample));
    }

    [Fact]
    public void DbDataReaderExtensions_ToObjectOverloads_MapWritableNonNullColumnsAndReuseCache()
    {
        using FakeDbDataReader reader = new(
            ["Title", "Subtitle", "ReadOnlyProp", "UnknownColumn"],
            ["Alpha", DBNull.Value, 99, "skip"]);

#pragma warning disable CS0618
        SetValuesTarget direct = reader.ToObject<SetValuesTarget>();
        Assert.Equal("Alpha", direct.Title);
        Assert.Equal(string.Empty, direct.Subtitle);
        Assert.Equal(7, direct.ReadOnlyProp);

        Dictionary<string, PropertyInfo?> cache = [];
        SetValuesTarget cached1 = reader.ToObject<SetValuesTarget>(cache);
        SetValuesTarget cached2 = reader.ToObject<SetValuesTarget>(cache);
#pragma warning restore CS0618

        Assert.Equal("Alpha", cached1.Title);
        Assert.Equal("Alpha", cached2.Title);
        Assert.True(cache.ContainsKey("UnknownColumn"));
        Assert.Null(cache["UnknownColumn"]);
    }

    [Fact]
    public void NpgsqlCommandExtensions_SetCommandParametersFromEntityAndCollection_HandleEmptyAndPopulated()
    {
        using NpgsqlCommand cmd = new();

        EnumAndNullableEntity? nullEntity = null;
        cmd.SetCommandParametersFromEntity(nullEntity);
        Assert.Empty(cmd.Parameters);

        EnumAndNullableEntity entity = new() { Id = 1, Mode = SampleEnum.Second, Notes = null };
        cmd.SetCommandParametersFromEntity(entity);
        Assert.Equal(3, cmd.Parameters.Count);
        Assert.Equal(DBNull.Value, cmd.Parameters["Notes"].Value);

        cmd.Parameters.Clear();
        cmd.SetCommandParameters([]);
        Assert.Empty(cmd.Parameters);

        cmd.SetCommandParameters([new NpgsqlParameter("P1", NpgsqlDbType.Text) { Value = "v" }]);
        Assert.Single(cmd.Parameters);
        Assert.Equal("v", cmd.Parameters["P1"].Value);
    }
}
