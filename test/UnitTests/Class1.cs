using MelloSilveiraTools.Infrastructure.Database.Attributes;
using MelloSilveiraTools.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.Infrastructure.Database.Sql.Provider;

namespace UnitTests;

/// <summary>
/// Smoke tests using the original domain entity graph (user / district / join table).
/// These verify that the SQL provider handles FK-heavy schemas end-to-end.
/// </summary>
public sealed class DomainEntitySqlSmokeTests
{
    private readonly ISqlProvider _provider = new PostgresSqlProvider();

    [Fact]
    public void UserManagedDistrict_SelectSql_ContainsJoinsForBothForeignKeys()
    {
        var select = _provider.GetSelectSql<UserManagedDistrictEntity>();

        Assert.Contains("FROM user_managed_district AS usmd", select);
        Assert.Contains("INNER JOIN prajah_user",             select);
        Assert.Contains("INNER JOIN district",                select);
    }

    [Fact]
    public void UserManagedDistrict_SelectDistinctSql_ContainsSelectDistinct()
    {
        var sql = _provider.GetSelectDistinctSql<UserManagedDistrictEntity>();

        Assert.Contains("SELECT DISTINCT", sql);
    }

    [Fact]
    public void UserManagedDistrict_InsertSql_ContainsForeignKeyColumns()
    {
        var sql = _provider.GetInsertSql<UserManagedDistrictEntity>();

        Assert.Contains("user_id",    sql);
        Assert.Contains("district_id", sql);
        Assert.Contains("RETURNING id", sql);
    }

    [Fact]
    public void District_InsertSql_ReturnsOnConflictInsert_BecauseOfUniqueColumns()
    {
        // DistrictEntity has several [UniqueColumn] properties — conflict clause uses their actual column names.
        var sql = _provider.GetInsertSql<DistrictEntity>();

        Assert.Contains("ON CONFLICT (name, city, state_abbreviation, region_abbreviation, country_abbreviation)", sql);
        Assert.Contains("DO UPDATE SET name = EXCLUDED.name", sql);
    }
}

// ── Entity definitions (kept here to preserve the original test intent) ────────

[Table("user_managed_district", "usmd")]
public record UserManagedDistrictEntity : EntityBase
{
    [ForeignKeyColumn(typeof(UserEntity))]
    public long UserId { get; init; }

    [ForeignKeyColumn(typeof(DistrictEntity))]
    public long DistrictId { get; init; }
}

[Table("prajah_user", "pusr")]
public record UserEntity : EntityBase
{
    [Column] public string Document         { get; init; } = null!;
    [Column] public string Name             { get; init; } = null!;
    [Column] public string Email            { get; init; } = null!;
    [Column] public string PasswordHash     { get; init; } = null!;
    [Column] public byte[] CompressedPhotoContent { get; init; } = null!;
    [Column] public string[] PhoneNumbers   { get; init; } = null!;
    [Column] public string[] InstagramAccounts { get; init; } = null!;
    [Column] public string[] FacebookAccounts  { get; init; } = null!;
    [Column] public string[] LinkedinAccounts  { get; init; } = null!;
}

[Table("district")]
public record DistrictEntity : EntityBase
{
    [UniqueColumn] public string Name                { get; init; } = null!;
    [UniqueColumn] public string City                { get; init; } = null!;
    [Column]       public string State               { get; init; } = null!;
    [UniqueColumn] public string StateAbbreviation   { get; init; } = null!;
    [Column]       public string Region              { get; init; } = null!;
    [UniqueColumn] public string RegionAbbreviation  { get; init; } = null!;
    [Column]       public string Country             { get; init; } = null!;
    [UniqueColumn] public string CountryAbbreviation { get; init; } = null!;
}
