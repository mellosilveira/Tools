using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;

namespace UnitTests.Database.RelationalDatabase.Sql.Provider;

public sealed class PostgresSqlProviderTests
{
    private readonly ISqlProvider _provider = new PostgresSqlProvider();

    [Table("unique_only_item", "uoi")]
    private sealed record UniqueOnlyEntity
    {
        [PrimaryKeyColumn]
        public long Id { get; init; }

        [UniqueColumn]
        public string Code { get; init; } = string.Empty;
    }

    [Table("left_joined_item", "lji")]
    private sealed record LeftJoinedEntity : EntityBase
    {
        [ForeignKeyColumn(typeof(ProductEntity), JoinType.Left)]
        public long? ProductId { get; init; }

        [UniqueColumn]
        public string ExternalKey { get; init; } = string.Empty;
    }

    [Fact]
    public void GetInsertSql_CoversPlainUniqueWithPayloadAndUniqueOnlyFallback()
    {
        string plainSql = _provider.GetInsertSql<ProductEntity>();
        Assert.Contains("INSERT INTO product", plainSql);
        Assert.Contains("id, creation_timestamp, name, price", plainSql);
        Assert.Contains("@Id, @CreationTimestamp, @Name, @Price", plainSql);
        Assert.Contains("RETURNING id", plainSql);
        Assert.DoesNotContain("ON CONFLICT", plainSql);

        string uniqueWithPayloadSql = _provider.GetInsertSql<CategoryEntity>();
        Assert.Contains("INSERT INTO category", uniqueWithPayloadSql);
        Assert.Contains("ON CONFLICT (code)", uniqueWithPayloadSql);
        Assert.Contains("creation_timestamp = EXCLUDED.creation_timestamp", uniqueWithPayloadSql);
        Assert.Contains("description = EXCLUDED.description", uniqueWithPayloadSql);

        string uniqueOnlySql = _provider.GetInsertSql<UniqueOnlyEntity>();
        Assert.Contains("ON CONFLICT (code)", uniqueOnlySql);
        Assert.Contains("DO UPDATE SET code = EXCLUDED.code", uniqueOnlySql);

        string storedFileSql = _provider.GetInsertSql<StorageEntryMetadataEntity>();
        Assert.Contains("INSERT INTO stored_files", storedFileSql);
        Assert.Contains("external_identifier", storedFileSql);
        Assert.Contains("storage_provider", storedFileSql);
    }

    [Fact]
    public void GetTryInsertSql_GeneratesConflictDetectionCte()
    {
        string sql = _provider.GetTryInsertSql<CategoryEntity>();

        Assert.Contains("INSERT INTO category", sql);
        Assert.Contains("ON CONFLICT (code)", sql);
        Assert.Contains("WHERE code = @Code", sql);
        Assert.Contains("RETURNING", sql);
    }

    [Fact]
    public void GetBulkInsertSql_CoversSingleMultiUniqueWithAndWithoutPayloadAndCaching()
    {
        string batchTwoSql = _provider.GetBulkInsertSql<ProductEntity>(2);
        Assert.Contains("@Id_1, @CreationTimestamp_1, @Name_1, @Price_1", batchTwoSql);
        Assert.Contains("@Id_2, @CreationTimestamp_2, @Name_2, @Price_2", batchTwoSql);
        Assert.DoesNotContain("@Id_0", batchTwoSql);
        Assert.DoesNotContain("@Id_3", batchTwoSql);
        Assert.DoesNotContain("ON CONFLICT", batchTwoSql);

        string uniqueBatchSql = _provider.GetBulkInsertSql<CategoryEntity>(1);
        Assert.Contains("ON CONFLICT (code)", uniqueBatchSql);
        Assert.Contains("DO UPDATE SET", uniqueBatchSql);
        Assert.Contains("description = EXCLUDED.description", uniqueBatchSql);

        string uniqueOnlyBatchSql = _provider.GetBulkInsertSql<UniqueOnlyEntity>(2);
        Assert.Contains("ON CONFLICT (code)", uniqueOnlyBatchSql);
        Assert.Contains("DO UPDATE SET code = EXCLUDED.code", uniqueOnlyBatchSql);

        Assert.Same(batchTwoSql, _provider.GetBulkInsertSql<ProductEntity>(2));
        Assert.NotEqual(batchTwoSql, _provider.GetBulkInsertSql<ProductEntity>(3));
    }

    [Fact]
    public void GetSelectVariants_CoverPlainDistinctPrimaryKeyUniqueAndForeignKeyJoins()
    {
        string selectSql = _provider.GetSelectSql<ProductEntity>();
        Assert.Contains("SELECT", selectSql);
        Assert.DoesNotContain("SELECT DISTINCT", selectSql);
        Assert.Contains("FROM product AS prd", selectSql);
        Assert.Contains("prd.id AS \"Id\"", selectSql);
        Assert.Contains("#WHERE", selectSql);

        string distinctSql = _provider.GetSelectDistinctSql<ProductEntity>();
        Assert.Contains("SELECT DISTINCT", distinctSql);
        Assert.Contains("FROM product AS prd", distinctSql);

        string byPkSql = _provider.GetSelectByPrimaryKeySql<ProductEntity>();
        Assert.Contains("WHERE prd.id = @Id", byPkSql);
        Assert.DoesNotContain("#WHERE", byPkSql);
        Assert.DoesNotContain("#ORDERBY", byPkSql);
        Assert.DoesNotContain("#LIMIT", byPkSql);
        Assert.DoesNotContain("#OFFSET", byPkSql);

        string byUniqueSql = _provider.GetSelectByUniqueColumnSql<CategoryEntity>();
        Assert.Contains("WHERE cat.code = @UniqueColumnValue", byUniqueSql);
        Assert.DoesNotContain("#WHERE", byUniqueSql);

        string innerJoinSql = _provider.GetSelectSql<OrderItemEntity>();
        Assert.Contains("INNER JOIN product AS prd ON prd.id = ordi.product_id", innerJoinSql);
        Assert.DoesNotContain("prd.name AS \"Name\"", innerJoinSql);

        string leftJoinSql = _provider.GetSelectSql<LeftJoinedEntity>();
        Assert.Contains("LEFT JOIN product AS prd ON prd.id = lji.product_id", leftJoinSql);
    }

    [Fact]
    public void GetCountExistDeleteAndUpdateVariants_GenerateExpectedPostgresSqlAndCacheInstances()
    {
        string countSql = _provider.GetCountSql<ProductEntity>();
        Assert.Contains("SELECT COUNT(1) FROM product AS prd", countSql);
        Assert.Contains("#WHERE", countSql);

        string existSql = _provider.GetExistByPrimaryKeySql<ProductEntity>();
        Assert.Contains("SELECT 1 FROM product AS prd", existSql);
        Assert.Contains("WHERE prd.id = @Id", existSql);
        Assert.Contains("LIMIT 1", existSql);

        string deleteSql = _provider.GetDeleteSql<ProductEntity>();
        Assert.Contains("DELETE FROM product AS prd", deleteSql);
        Assert.Contains("#WHERE", deleteSql);

        string deletePkSql = _provider.GetDeleteByPrimaryKeySql<ProductEntity>();
        Assert.Contains("DELETE FROM product AS prd", deletePkSql);
        Assert.Contains("WHERE prd.id = @Id", deletePkSql);
        Assert.DoesNotContain("#WHERE", deletePkSql);

        string updateSql = _provider.GetUpdateSql<ProductEntity>();
        Assert.Contains("UPDATE product", updateSql);
        Assert.Contains("name = @Name", updateSql);
        Assert.Contains("price = @Price", updateSql);
        Assert.Contains("#WHERE", updateSql);

        string updatePkSql = _provider.GetUpdateByPrimaryKeySql<ProductEntity>();
        Assert.Contains("UPDATE product", updatePkSql);
        Assert.Contains("WHERE id = @Id;", updatePkSql);
        Assert.DoesNotContain("#WHERE", updatePkSql);

        Assert.Same(_provider.GetInsertSql<ProductEntity>(), _provider.GetInsertSql<ProductEntity>());
    }

    [Fact]
    public void MetadataValidation_ThrowsOnMissingTableMissingPrimaryKeyOrMissingUniqueColumn()
    {
        Assert.Throws<InvalidOperationException>(() => _provider.GetInsertSql<NoTableEntity>());
        Assert.Throws<InvalidOperationException>(() => _provider.GetInsertSql<NoPrimaryKeyEntity>());
        Assert.Throws<ArgumentOutOfRangeException>(() => _provider.GetSelectByUniqueColumnSql<ProductEntity>());
    }
}
