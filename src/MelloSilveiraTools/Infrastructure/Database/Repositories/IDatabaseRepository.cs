using MelloSilveiraTools.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.Infrastructure.Database.Models.Filters;

namespace MelloSilveiraTools.Infrastructure.Database.Repositories;

/// <summary>
/// Repository that contains methods to deal with database.
/// </summary>
public interface IDatabaseRepository
{
    Task<bool> ExistAsync<TEntity>(long id) where TEntity : EntityBase;

    Task<bool> ExistAsync<TEntity, TFilter>(TFilter filter) where TEntity : EntityBase;

    Task<TEntity?> GetFirstOrDefaultAsync<TEntity, TFilter>(TFilter filter)
        where TEntity : EntityBase, new()
        where TFilter : FilterBase;

    Task<TEntity?> GetAsync<TEntity>(long id) where TEntity : EntityBase;

    IAsyncEnumerable<TEntity> GetAsync<TEntity, TFilter>(TFilter filter)
        where TEntity : EntityBase, new()
        where TFilter : FilterBase;

    IAsyncEnumerable<TEntity> GetDistinctAsync<TEntity, TFilter>(TFilter filter)
        where TEntity : EntityBase, new()
        where TFilter : FilterBase;

    Task DeleteAllAsync<TEntity>() where TEntity : EntityBase;

    Task DeleteAsync<TEntity>(long id) where TEntity : EntityBase;

    Task DeleteAsync<TEntity, TFilter>(TFilter filter) where TEntity : EntityBase;

    Task<long> InsertAsync<TEntity>(TEntity entity) where TEntity : EntityBase;

    Task<long[]> InsertAsync<TEntity>(TEntity[] entity) where TEntity : EntityBase;

    Task<long[]> UpsertAsync<TEntity, TFilter>(TEntity[] entity, TFilter filter) where TEntity : EntityBase;

    Task UpdateAsync<TEntity>(TEntity entity) where TEntity : EntityBase;
}
