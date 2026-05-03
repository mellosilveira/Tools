using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;

namespace MelloSilveiraTools.WebApi.Application.Operations.Crud.Update;

/// <summary>
/// Generic operation that updates an existing entity through <see cref="IRepository"/>.
/// Returns 201 Created when the row was modified or 204 No Content when no row matched the identifier.
/// Shared between <c>CrudController</c> and <c>CrudEndpoints</c>.
/// </summary>
/// <typeparam name="TEntity">Entity type being updated.</typeparam>
public class UpdateEntity<TEntity>(ILogger logger, IRepository repository)
    : OperationBaseWithDefaultResponse<UpdateEntityRequest<TEntity>>(logger)
    where TEntity : EntityBase, new()
{
    /// <inheritdoc />
    protected override Task<OperationResponse> ValidateOperationAsync(UpdateEntityRequest<TEntity> request)
        => OperationResponse.CreateSuccessOk().AsTask();

    /// <inheritdoc />
    protected override async Task<OperationResponse> ProcessOperationAsync(UpdateEntityRequest<TEntity> request)
    {
        try
        {
            TEntity entityToUpdate = request.Entity with { Id = request.Id };
            return await repository.TryUpdateAsync(entityToUpdate).ConfigureAwait(false)
                ? OperationResponse.CreateSuccessCreated()
                : OperationResponse.CreateNoContent();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao atualizar um(a) {request.ResourceName}.";
            Dictionary<string, object?> logAdditionalData = new() { { "Id", request.Id }, { "Entity", request.Entity } };
            Logger.Error(message, ex, logAdditionalData);
            return OperationResponse.CreateInternalServerError(message);
        }
    }
}
