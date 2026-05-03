using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;

namespace MelloSilveiraTools.WebApi.Application.Operations.Crud.Read;

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
    protected override Task<OperationResponse<TEntity>> ValidateOperationAsync(ReadEntityByIdRequest request) => OperationResponse.CreateSuccessOk<TEntity>().AsTask();

    /// <inheritdoc />
    protected override async Task<OperationResponse<TEntity>> ProcessOperationAsync(ReadEntityByIdRequest request)
    {
        try
        {
            TEntity? entity = await repository.GetAsync<TEntity>(request.Id).ConfigureAwait(false);
            return entity is null ? OperationResponse.CreateNotFound() : OperationResponse.CreateSuccessOk(entity);
        }
        catch (Exception ex)
        {
            string message = $"Falha ao buscar {request.ResourceName} pelo identificador.";

            Dictionary<string, object?> logAdditionalData = new() { { "Id", request.Id } };
            Logger.Error(message, ex, logAdditionalData);

            return OperationResponse.CreateInternalServerError(message);
        }
    }
}
