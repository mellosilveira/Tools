using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using System.Net;

namespace MelloSilveiraTools.WebApi.Application.Operations.Crud.Read;

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
    protected override Task<PagedOperationResponse<TEntity>> ValidateOperationAsync(ReadEntityPagedRequest<TFilter> request) => OperationResponse.CreatePagedSuccessOk<TEntity>().AsTask();

    /// <inheritdoc />
    protected override async Task<PagedOperationResponse<TEntity>> ProcessOperationAsync(ReadEntityPagedRequest<TFilter> request)
    {
        try
        {
            long totalCount = await repository.CountAsync<TEntity, TFilter>(request.Filter).ConfigureAwait(false);
            var entities = await repository
                .GetAsync<TEntity, TFilter>(request.Filter, request.Pagination)
                .ToListAsync(request.CancellationToken)
                .ConfigureAwait(false);

            return new PagedOperationResponse<TEntity>
            {
                StatusCode = HttpStatusCode.OK,
                Success = true,
                Data = entities,
                TotalCount = totalCount,
                PageNumber = entities.Count > 0 ? (request.Pagination.Offset ?? 0) / entities.Count + 1 : 1,
            };
        }
        catch (Exception ex)
        {
            string message = $"Falha ao buscar {request.ResourceName}.";
            
            Dictionary<string, object?> logAdditionalData = new() { { "Filter", request.Filter } };
            Logger.Error(message, ex, logAdditionalData);

            return OperationResponse.CreateInternalServerError(message);
        }
    }
}
