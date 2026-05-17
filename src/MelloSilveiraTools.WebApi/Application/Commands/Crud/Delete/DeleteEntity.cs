using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;

namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Delete;

/// <summary>
/// Generic operation that deletes an entity by its identifier through <see cref="IRepository"/>.
/// Shared between <c>CrudController</c> and <c>CrudEndpoints</c>.
/// </summary>
/// <typeparam name="TEntity">Entity type being deleted.</typeparam>
public class DeleteEntity<TEntity>(ILogger logger, IRepository repository)
    : CommandBaseWithDefaultResponse<DeleteEntityRequest>(logger)
    where TEntity : EntityBase, new()
{
    /// <inheritdoc />
    protected override async Task<Result> ExecuteCommandAsync(DeleteEntityRequest request)
    {
        try
        {
            await repository.DeleteAsync<TEntity>(request.Id).ConfigureAwait(false);
            return Result.CreateSuccessOk();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao deletar um(a) {request.ResourceName}.";
            
            Dictionary<string, object?> logAdditionalData = new() { { "Id", request.Id } };
            Logger.Error(message, ex, logAdditionalData);
            
            return Result.CreateUnknownError(message);
        }
    }
}
