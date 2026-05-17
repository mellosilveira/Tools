using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;

namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Update;

/// <summary>
/// Generic operation that updates an existing entity through <see cref="IRepository"/>.
/// Returns 201 Created when the row was modified or 204 No Content when no row matched the identifier.
/// Shared between <c>CrudController</c> and <c>CrudEndpoints</c>.
/// </summary>
/// <typeparam name="TEntity">Entity type being updated.</typeparam>
public class UpdateEntity<TEntity>(ILogger logger, IRepository repository)
    : CommandBaseWithDefaultResponse<UpdateEntityRequest<TEntity>>(logger)
    where TEntity : EntityBase, new()
{
    /// <inheritdoc />
    protected override async Task<Result> ExecuteCommandAsync(UpdateEntityRequest<TEntity> request)
    {
        try
        {
            TEntity entityToUpdate = request.Entity with { Id = request.Id };
            return await repository.TryUpdateAsync(entityToUpdate).ConfigureAwait(false)
                ? Result.CreateSuccessCreated()
                : Result.CreateNoContent();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao atualizar um(a) {request.ResourceName}.";
            
            Dictionary<string, object?> logAdditionalData = new() { { "Id", request.Id }, { "Entity", request.Entity } };
            Logger.Error(message, ex, logAdditionalData);
            
            return Result.CreateUnknownError(message);
        }
    }
}
