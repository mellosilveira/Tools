using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Database.Domain.Repositories;
using MelloSilveiraTools.Database.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.Database.Infrastructure.Database.Models.Filters;
using System.Net;

namespace MelloSilveiraTools.WebApi.Application.Operations.Crud;

/// <summary>
/// Generic operation that returns a paginated list of entities matching the supplied filter.
/// Shared between <c>CrudController</c> and <c>CrudEndpoints</c>.
/// </summary>
/// <typeparam name="TEntity">Entity type being queried.</typeparam>
/// <typeparam name="TFilter">Filter type used to query the entity.</typeparam>
public class ReadEntityPaged<TEntity, TFilter>(ILogger logger, IRepository repository)
    : PagedOperationBase<ReadEntityPagedRequest<TFilter>, TEntity>(logger)
    where TEntity : EntityBase, new()
    where TFilter : FilterBase, new()
{
    /// <inheritdoc />
    protected override Task<OperationPagedResponseBase<TEntity>> ValidateOperationAsync(ReadEntityPagedRequest<TFilter> request)
        => Task.FromResult<OperationPagedResponseBase<TEntity>>(new() { StatusCode = HttpStatusCode.OK, Success = true });

    /// <inheritdoc />
    protected override async Task<OperationPagedResponseBase<TEntity>> ProcessOperationAsync(ReadEntityPagedRequest<TFilter> request)
    {
        try
        {
            long totalCount = await repository.CountAsync<TEntity, TFilter>(request.Filter).ConfigureAwait(false);
            TEntity[] entities = await repository
                .GetAsync<TEntity, TFilter>(request.Filter, request.Pagination)
                .ToArrayAsync(request.CancellationToken)
                .ConfigureAwait(false);

            return new OperationPagedResponseBase<TEntity>
            {
                StatusCode = HttpStatusCode.OK,
                Success = true,
                Data = entities,
                TotalCount = totalCount,
                PageSize = entities.LongLength,
                PageNumber = entities.LongLength > 0 ? (request.Pagination.Offset ?? 0) / entities.LongLength + 1 : 1,
            };
        }
        catch (Exception ex)
        {
            string message = $"Falha ao buscar {request.ResourceName}.";
            Dictionary<string, object?> logAdditionalData = new() { { "Filter", request.Filter } };
            Logger.Error(message, ex, logAdditionalData);
            return CreateError(HttpStatusCode.InternalServerError, message);
        }
    }
}
