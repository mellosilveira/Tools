using MelloSilveiraTools.Infrastructure.Database.Attributes;
using Newtonsoft.Json;

namespace MelloSilveiraTools.Infrastructure.Database.Models.Entities;

/// <summary>
/// Represents the base entity for database.
/// </summary>
public abstract record EntityBase
{
    [JsonIgnore]
    [PrimaryKeyColumn]
    public long Id { get; init; }

    [Column]
    public DateTimeOffset CreationTimestamp { get; init; }
}
