using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Database.Domain.Repositories;
using MelloSilveiraTools.Database.Infrastructure.Database.Models.Entities;
using System.Net;

namespace MelloSilveiraTools.WebApi.Application.Operations.Crud;

/// <summary>
/// Generic operation that loads a single entity by its identifier through <see cref="IRepository"/>.
/// Shared between <c>CrudController</c> and <c>CrudEndpoints</c>.
/// </summary>
/// <typeparam name="TEntity">Entity type being read.</typeparam>
public class ReadEntityById<TEntity>(ILogger logger, IRepository repository)
    : OperationBaseWithData<ReadEntityByIdRequest, TEntity>(logger)
    where TEntity : EntityBase, new()
{
    /// <inheritdoc />
    protected override Task<OperationResponseBase<TEntity>> ValidateOperationAsync(ReadEntityByIdRequest request)
        => Task.FromResult(CreateSuccessOk());

    /// <inheritdoc />
    protected override async Task<OperationResponseBase<TEntity>> ProcessOperationAsync(ReadEntityByIdRequest request)
    {
        try
        {
            TEntity? entity = await repository.GetAsync<TEntity>(request.Id).ConfigureAwait(false);
            return CreateSuccessOk(entity);
        }
        catch (Exception ex)
        {
            string message = $"Falha ao buscar {request.ResourceName} pelo identificador.";
            Dictionary<string, object?> logAdditionalData = new() { { "Id", request.Id } };
            Logger.Error(message, ex, logAdditionalData);
            return CreateError(HttpStatusCode.InternalServerError, message);
        }
    }
}
