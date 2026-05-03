using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;

namespace UnitTests;

// ── Simple entity: no unique columns ──────────────────────────────────────────
[Table("product", "prd")]
public record ProductEntity : EntityBase
{
    [Column] public string Name { get; init; } = null!;
    [Column] public long Price { get; init; }
}

// ── Entity with unique column ──────────────────────────────────────────────────
[Table("category", "cat")]
public record CategoryEntity : EntityBase
{
    [UniqueColumn] public string Code { get; init; } = null!;
    [Column]       public string Description { get; init; } = null!;
}

// ── Entity with a foreign key → tests JOIN generation ─────────────────────────
[Table("order_item", "ordi")]
public record OrderItemEntity : EntityBase
{
    [ForeignKeyColumn(typeof(ProductEntity))]
    public long ProductId { get; init; }

    [Column] public int Quantity { get; init; }
}

// ── Error-condition entities ───────────────────────────────────────────────────

// No TableAttribute → should throw InvalidOperationException
public record NoTableEntity { }

// Has TableAttribute but no PrimaryKeyColumn → should throw InvalidOperationException
[Table("no_pk", "npk")]
public record NoPrimaryKeyEntity
{
    [Column] public string Name { get; init; } = null!;
}
