using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;

namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;

/// <summary>
/// Generic operation that returns a paginated list of entities matching the supplied filter.
/// Shared between <c>CrudController</c> and <c>CrudEndpoints</c>.
/// </summary>
/// <typeparam name="TEntity">Entity type being queried.</typeparam>
/// <typeparam name="TFilter">Filter type used to query the entity.</typeparam>
public class ReadEntityPaged<TEntity, TFilter>(IRepository repository) : PagedCommandBase<ReadEntityPagedRequest<TFilter>, TEntity> where TEntity : EntityBase, new()
    where TFilter : FilterBase, new()
{
    /// <inheritdoc />
    protected override async Task<PagedResult<TEntity>> ExecuteCommandAsync(ReadEntityPagedRequest<TFilter> request)
    {
        long totalCount = await repository.CountAsync<TEntity, TFilter>(request.Filter).ConfigureAwait(false);
        var entities = await repository
            .GetAsync<TEntity, TFilter>(request.Filter, request.Pagination)
            .ToListAsync(request.CancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<TEntity>
        {
            StatusCode = StatusCode.OK,
            Success = true,
            Data = entities,
            TotalCount = totalCount,
            PageNumber = entities.Count > 0 ? (request.Pagination.Offset ?? 0) / entities.Count + 1 : 1,
        };
    }
}
