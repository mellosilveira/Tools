using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NpgsqlTypes;
using Polly;
using System.Collections;
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

        ProductEntity product = new() { Id = 5, CreationTimestamp = DateTimeOffset.UtcNow, Name = "ACL", Price = 100 };
        List<NpgsqlParameter> declaredOnly = [.. product.BuildParameters(useDeclaredProperties: true)];
        List<NpgsqlParameter> withInherited = [.. product.BuildParameters(useDeclaredProperties: false)];
        Assert.Equal(["Name", "Price"], declaredOnly.Select(p => p.ParameterName).ToArray());
        Assert.Equal(4, withInherited.Count);

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
    public void ClassExtensions_BuildWhereClauseAndNpgsqlParameters_CoverNullsMissingAttrEmptyJoinedAndPatterns()
    {
        OrderItemSearchFilter? nullFilter = null;
        (string? nullNpgSql, List<NpgsqlParameter> nullNpgParams) = nullFilter.BuildWhereClauseAndNpgsqlParameters();
        Assert.Null(nullNpgSql);
        Assert.Empty(nullNpgParams);

        UnattributedFilter noAttr = new() { Anything = "x" };
        (string? noAttrSql, List<NpgsqlParameter> noAttrParams) = noAttr.BuildWhereClauseAndNpgsqlParameters();
        Assert.Null(noAttrSql);
        Assert.Empty(noAttrParams);

        OrderItemSearchFilter blankFilter = new() { Quantity = null, ProductNamePattern = "   ", Name = null };
        (string? blankSql, List<NpgsqlParameter> blankParams) = blankFilter.BuildWhereClauseAndNpgsqlParameters();
        Assert.Null(blankSql);
        Assert.Empty(blankParams);

        OrderItemSearchFilter active = new()
        {
            Quantity = 3,
            ProductNamePattern = "lig",
            Name = "knee",
            Status = SampleEnum.First
        };

        (string? whereNpgsql, List<NpgsqlParameter> npgsqlParams) = active.BuildWhereClauseAndNpgsqlParameters();
        Assert.NotNull(whereNpgsql);
        Assert.Contains("WHERE ordi.quantity = @Quantity", whereNpgsql);
        Assert.Contains("prd.name LIKE @ProductNamePattern", whereNpgsql);
        Assert.Contains("prd.name ILIKE @Name", whereNpgsql);
        Assert.Contains("ordi.status = @Status", whereNpgsql);
        Assert.Equal(4, npgsqlParams.Count);
        Assert.Equal("%lig%", npgsqlParams.Single(p => p.ParameterName == "ProductNamePattern").Value);
        Assert.Equal("%knee%", npgsqlParams.Single(p => p.ParameterName == "Name").Value);
        Assert.Equal((int)SampleEnum.First, npgsqlParams.Single(p => p.ParameterName == "Status").Value);
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
    public void NpgsqlCommandExtensions_SetCommandParametersFromEntityAndCollection_HandleEmptyAndPopulated()
    {
        using NpgsqlCommand cmd = new();

        EnumAndNullableEntity? nullEntity = null;
        cmd.SetCommandParametersFromEntity(nullEntity);
        Assert.Empty(cmd.Parameters);

        EnumAndNullableEntity entity = new() { Id = 1, Mode = SampleEnum.Second, Notes = null };
        cmd.SetCommandParametersFromEntity(entity);
        Assert.Equal(3, cmd.Parameters.Count);
        Assert.Equal((int)SampleEnum.Second, cmd.Parameters["Mode"].Value);
        Assert.Equal(DBNull.Value, cmd.Parameters["Notes"].Value);

        cmd.Parameters.Clear();
        cmd.SetCommandParameters([]);
        Assert.Empty(cmd.Parameters);

        cmd.SetCommandParameters([new NpgsqlParameter("P1", NpgsqlDbType.Text) { Value = "v" }]);
        Assert.Single(cmd.Parameters);
        Assert.Equal("v", cmd.Parameters["P1"].Value);
    }

    [Fact]
    public void NpgsqlBatchCommandExtensions_SetCommandParameters_HandlesNullEmptyDirectAndClonedAcrossListAndEnumerable()
    {
        NpgsqlBatchCommand nullCmd = new("SELECT 1");
        Assert.Same(nullCmd, nullCmd.SetCommandParameters(null));
        Assert.Empty(nullCmd.Parameters);

        NpgsqlBatchCommand emptyCmd = new("SELECT 1");
        Assert.Same(emptyCmd, emptyCmd.SetCommandParameters([]));
        Assert.Empty(emptyCmd.Parameters);

        // IReadOnlyList fast path: direct bind (cloneParameters: false) then retry clone (cloneParameters: true)
        NpgsqlParameter listParam = new("@Id", NpgsqlDbType.Bigint) { Value = 42L };
        List<NpgsqlParameter> listSource = [listParam];

        NpgsqlBatchCommand directListCmd = new NpgsqlBatchCommand("SELECT @Id").SetCommandParameters(listSource, cloneParameters: false);
        Assert.Single(directListCmd.Parameters);
        Assert.Same(listParam, directListCmd.Parameters[0]);

        NpgsqlBatchCommand clonedListCmd = new NpgsqlBatchCommand("SELECT @Id").SetCommandParameters(listSource, cloneParameters: true);
        Assert.Single(clonedListCmd.Parameters);
        Assert.NotSame(listParam, clonedListCmd.Parameters[0]);
        Assert.Equal("@Id", clonedListCmd.Parameters[0].ParameterName);
        Assert.Equal(NpgsqlDbType.Bigint, clonedListCmd.Parameters[0].NpgsqlDbType);
        Assert.Equal(42L, clonedListCmd.Parameters[0].Value);

        // General IEnumerable (non-IReadOnlyList) path: direct bind and clone
        NpgsqlParameter seqParam = new("@Code", NpgsqlDbType.Text) { Value = "ACL" };
        IEnumerable<NpgsqlParameter> enumerableSource = YieldSingle(seqParam);

        NpgsqlBatchCommand directSeqCmd = new NpgsqlBatchCommand("SELECT @Code").SetCommandParameters(enumerableSource, cloneParameters: false);
        Assert.Single(directSeqCmd.Parameters);
        Assert.Same(seqParam, directSeqCmd.Parameters[0]);

        NpgsqlBatchCommand clonedSeqCmd = new NpgsqlBatchCommand("SELECT @Code").SetCommandParameters(YieldSingle(seqParam), cloneParameters: true);
        Assert.Single(clonedSeqCmd.Parameters);
        Assert.NotSame(seqParam, clonedSeqCmd.Parameters[0]);
        Assert.Equal("@Code", clonedSeqCmd.Parameters[0].ParameterName);
        Assert.Equal(NpgsqlDbType.Text, clonedSeqCmd.Parameters[0].NpgsqlDbType);
        Assert.Equal("ACL", clonedSeqCmd.Parameters[0].Value);

        static IEnumerable<NpgsqlParameter> YieldSingle(NpgsqlParameter p)
        {
            yield return p;
        }
    }

    [Fact]
    public async Task NpgsqlDataSourceAndBatchExtensions_ConfigureTimeoutContextAndCommands()
    {
        await using NpgsqlDataSource dataSource = new NpgsqlSlimDataSourceBuilder("Host=localhost;Database=test;Username=postgres").Build();

        await using NpgsqlBatch defaultTimeoutBatch = dataSource.CreateBatch(null);
        Assert.Equal(30, defaultTimeoutBatch.Timeout);

        NpgsqlParameter originalParam = new("@Id", NpgsqlDbType.Bigint) { Value = 7L };

        await using NpgsqlBatch configuredBatch = dataSource
            .CreateBatch(15)
            .AddUserContext(42L)
            .AddCommand("SELECT @Id", [originalParam], cloneParameters: true);

        Assert.Equal(15, configuredBatch.Timeout);
        Assert.Equal(2, configuredBatch.BatchCommands.Count);
        Assert.Contains("set_config('app.current_user_id'", configuredBatch.BatchCommands[0].CommandText);
        Assert.Equal("42", configuredBatch.BatchCommands[0].Parameters["@UserId"].Value);
        Assert.Equal("SELECT @Id", configuredBatch.BatchCommands[1].CommandText);
        Assert.NotSame(originalParam, configuredBatch.BatchCommands[1].Parameters["@Id"]);
        Assert.Equal(7L, configuredBatch.BatchCommands[1].Parameters["@Id"].Value);

        await using NpgsqlBatch anonBatch = dataSource.CreateBatch(null).AddUserContext(null);
        Assert.Equal(string.Empty, anonBatch.BatchCommands[0].Parameters["@UserId"].Value);
    }

    [Fact]
    public async Task DefaultResiliencePipelineExtensions_ExecuteWithRetryFlagAsync_PassesFalseThenTrueOnRetry()
    {
        ResiliencePipelineSettings settings = new()
        {
            BackoffType = DelayBackoffType.Constant,
            DelayInMilliseconds = 1,
            MaxRetryAttempts = 2,
            UseJitter = false
        };
        DefaultResiliencePipeline pipeline = new(
            NullLogger<DefaultResiliencePipeline>.Instance,
            settings,
            new PredicateBuilder().Handle<InvalidOperationException>());

        List<bool> genericFlags = [];
        int result = await pipeline.ExecuteWithRetryFlagAsync(isRetry =>
        {
            genericFlags.Add(isRetry);
            if (!isRetry)
                throw new InvalidOperationException("transient");

            return ValueTask.FromResult(99);
        });

        Assert.Equal(99, result);
        Assert.Equal([false, true], genericFlags);

        List<bool> voidFlags = [];
        await pipeline.ExecuteWithRetryFlagAsync(isRetry =>
        {
            voidFlags.Add(isRetry);
            if (!isRetry)
                throw new InvalidOperationException("transient");

            return ValueTask.CompletedTask;
        });

        Assert.Equal([false, true], voidFlags);
    }
}
