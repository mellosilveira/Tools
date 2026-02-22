using MelloSilveiraTools.Infrastructure.Database.Attributes;
using MelloSilveiraTools.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.Infrastructure.Database.Sql.Provider;

namespace UnitTests;

public class Class1
{
    [Fact]
    public void A()
    {
        var sqlProvider = new NEW_PostgresSqlProvider();

        var select = sqlProvider.GetSelectSql<UserManagedDistrictEntity>();
        var selectDistinct = sqlProvider.GetSelectDistinctSql<UserManagedDistrictEntity>();
        var insert = sqlProvider.GetInsertSql<UserManagedDistrictEntity>();
    }
}

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
    //[Column]
    //public UserType Type { get; init; }

    //[Column]
    //public PersonType PersonType { get; init; }

    [Column]
    public string Document { get; init; }

    [Column]
    public string Name { get; init; }

    [Column]
    public string Email { get; init; }

    [Column]
    public string PasswordHash { get; init; }

    //[Column]
    //public Status Status { get; init; }

    [Column]
    public byte[] CompressedPhotoContent { get; init; }

    [Column]
    public string[] PhoneNumbers { get; init; }

    [Column]
    public string[] InstagramAccounts { get; init; }

    [Column]
    public string[] FacebookAccounts { get; init; }

    [Column]
    public string[] LinkedinAccounts { get; init; }

    //[ForeignKeyColumn(typeof(BankAccountEntity), JoinType.Left)]
    //public long? BankAccountId { get; init; }
}

[Table("district")]
public record DistrictEntity : EntityBase
{
    [UniqueColumn]
    public string Name { get; init; }

    [UniqueColumn]
    public string City { get; init; }

    [Column]
    public string State { get; init; }

    [UniqueColumn]
    public string StateAbbreviation { get; init; }

    [Column]
    public string Region { get; init; }

    [UniqueColumn]
    public string RegionAbbreviation { get; init; }

    [Column]
    public string Country { get; init; }

    [UniqueColumn]
    public string CountryAbbreviation { get; init; }
}