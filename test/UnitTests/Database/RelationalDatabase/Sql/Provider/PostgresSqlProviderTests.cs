using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;

namespace UnitTests;

public sealed class PostgresSqlProviderTests
{
    private readonly ISqlProvider _provider = new PostgresSqlProvider();

    // ── INSERT ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetInsertSql_NoUniqueColumns_ReturnsPlainInsert()
    {
        var sql = _provider.GetInsertSql<ProductEntity>();

        Assert.Contains("INSERT INTO product", sql);
        Assert.Contains("id, creation_timestamp, name, price", sql);
        Assert.Contains("@Id, @CreationTimestamp, @Name, @Price", sql);
        Assert.Contains("RETURNING id", sql);
        Assert.DoesNotContain("ON CONFLICT", sql);
    }

    //[Fact]
    //public void GetInsertSql_WithUniqueColumn_ReturnsOnConflictInsert()
    //{
    //    var sql = _provider.GetInsertSql<CategoryEntity>();

    //    Assert.Contains("INSERT INTO category", sql);
    //    Assert.Contains("ON CONFLICT (code)", sql);
    //    Assert.Contains("DO UPDATE SET code = EXCLUDED.code", sql);
    //    Assert.Contains("RETURNING id", sql);
    //}

    // ── BULK INSERT ────────────────────────────────────────────────────────────

    [Fact]
    public void GetBulkInsertSql_TwoRows_ContainsOneBased1and2Suffixes()
    {
        var sql = _provider.GetBulkInsertSql<ProductEntity>(2);

        Assert.Contains("INSERT INTO product", sql);
        Assert.Contains("@Id_1, @CreationTimestamp_1, @Name_1, @Price_1", sql);
        Assert.Contains("@Id_2, @CreationTimestamp_2, @Name_2, @Price_2", sql);
        Assert.DoesNotContain("@Id_0", sql); // no zero-based suffix
        Assert.DoesNotContain("@Id_3", sql); // only 2 rows
        Assert.Contains("RETURNING id", sql);
        Assert.DoesNotContain("ON CONFLICT", sql);
    }

    //[Fact]
    //public void GetBulkInsertSql_WithUniqueColumn_ReturnsOnConflict()
    //{
    //    var sql = _provider.GetBulkInsertSql<CategoryEntity>(1);

    //    Assert.Contains("@Id_1, @CreationTimestamp_1, @Code_1, @Description_1", sql);
    //    Assert.Contains("ON CONFLICT (code)", sql);
    //    Assert.Contains("DO UPDATE SET code = EXCLUDED.code", sql);
    //}

    [Fact]
    public void GetBulkInsertSql_DifferentBatchSizes_ProduceDifferentSql()
    {
        var sql1 = _provider.GetBulkInsertSql<ProductEntity>(1);
        var sql3 = _provider.GetBulkInsertSql<ProductEntity>(3);

        Assert.DoesNotContain("@Id_2", sql1);
        Assert.Contains("@Id_3", sql3);
        Assert.NotEqual(sql1, sql3);
    }

    // ── SELECT ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetSelectSql_ReturnsSelectWithoutDistinct()
    {
        var sql = _provider.GetSelectSql<ProductEntity>();

        Assert.Contains("SELECT", sql);
        Assert.DoesNotContain("SELECT DISTINCT", sql);
        Assert.Contains("FROM product AS prd", sql);
        Assert.Contains("prd.id AS \"Id\"", sql);
        Assert.Contains("prd.name AS \"Name\"", sql);
        Assert.Contains("prd.price AS \"Price\"", sql);
        Assert.Contains("prd.creation_timestamp AS \"CreationTimestamp\"", sql);
    }

    [Fact]
    public void GetSelectDistinctSql_ReturnsSelectDistinct()
    {
        var sql = _provider.GetSelectDistinctSql<ProductEntity>();

        Assert.Contains("SELECT DISTINCT", sql);
        Assert.Contains("FROM product AS prd", sql);
    }

    [Fact]
    public void GetSelectSql_EntityWithForeignKey_IncludesInnerJoin()
    {
        var sql = _provider.GetSelectSql<OrderItemEntity>();

        Assert.Contains("FROM order_item AS ordi", sql);
        Assert.Contains("INNER JOIN product AS prd ON prd.id = ordi.product_id", sql);
        Assert.Contains("ordi.quantity AS \"Quantity\"", sql);
        // Joined entity columns are NOT added to SELECT to avoid duplicate "Id"/"CreationTimestamp" aliases.
        Assert.DoesNotContain("prd.name AS \"Name\"", sql);
    }

    [Fact]
    public void GetSelectByPrimaryKeySql_ContainsWherePkAndNoPlaceholders()
    {
        var sql = _provider.GetSelectByPrimaryKeySql<ProductEntity>();

        Assert.Contains("WHERE prd.id = @Id", sql);
        Assert.DoesNotContain("#WHERE", sql);
        Assert.DoesNotContain("#ORDERBY", sql);
        Assert.DoesNotContain("#OFFSET", sql);
        Assert.DoesNotContain("#LIMIT", sql);
    }

    // ── COUNT / EXIST ──────────────────────────────────────────────────────────

    [Fact]
    public void GetCountSql_ContainsCountAndWherePlaceholder()
    {
        var sql = _provider.GetCountSql<ProductEntity>();

        Assert.Contains("SELECT COUNT(1) FROM product AS prd", sql);
        Assert.Contains("#WHERE", sql);
    }

    [Fact]
    public void GetExistByPrimaryKeySql_ContainsSelectOneWhereAndLimit()
    {
        var sql = _provider.GetExistByPrimaryKeySql<ProductEntity>();

        Assert.Contains("SELECT 1 FROM product AS prd", sql);
        Assert.Contains("WHERE prd.id = @Id", sql);
        Assert.Contains("LIMIT 1", sql);
    }

    // ── DELETE ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetDeleteSql_ContainsDeleteAndWherePlaceholder()
    {
        var sql = _provider.GetDeleteSql<ProductEntity>();

        Assert.Contains("DELETE FROM product AS prd", sql);
        Assert.Contains("#WHERE", sql);
    }

    [Fact]
    public void GetDeleteByPrimaryKeySql_ContainsWherePkAndNoPlaceholder()
    {
        var sql = _provider.GetDeleteByPrimaryKeySql<ProductEntity>();

        Assert.Contains("DELETE FROM product AS prd", sql);
        Assert.Contains("WHERE prd.id = @Id", sql);
        Assert.DoesNotContain("#WHERE", sql);
    }

    // ── UPDATE ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetUpdateByPrimaryKeySql_ContainsSetClauseAndWherePk()
    {
        var sql = _provider.GetUpdateByPrimaryKeySql<ProductEntity>();

        Assert.Contains("UPDATE product", sql);
        Assert.Contains("name = @Name", sql);
        Assert.Contains("price = @Price", sql);
        Assert.Contains("WHERE id = @Id", sql);
        Assert.DoesNotContain("#WHERE", sql);
    }

    // ── CACHING ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetInsertSql_CalledTwice_ReturnsSameStringInstance()
    {
        // Static cache means the second call returns the same object reference.
        var sql1 = _provider.GetInsertSql<ProductEntity>();
        var sql2 = _provider.GetInsertSql<ProductEntity>();

        Assert.Same(sql1, sql2);
    }

    [Fact]
    public void GetBulkInsertSql_SameBatchSizeCalledTwice_ReturnsSameStringInstance()
    {
        var sql1 = _provider.GetBulkInsertSql<ProductEntity>(5);
        var sql2 = _provider.GetBulkInsertSql<ProductEntity>(5);

        Assert.Same(sql1, sql2);
    }

    // ── ERROR CASES ────────────────────────────────────────────────────────────

    [Fact]
    public void GetInsertSql_EntityWithoutTableAttribute_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _provider.GetInsertSql<NoTableEntity>());
    }

    [Fact]
    public void GetInsertSql_EntityWithoutPrimaryKey_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _provider.GetInsertSql<NoPrimaryKeyEntity>());
    }
}
