using MelloSilveiraTools.Core.Infrastructure.Logger;
using MelloSilveiraTools.Database.Domain.Repositories;
using MelloSilveiraTools.Database.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.Database.ExtensionMethods;

namespace MelloSilveiraTools.WebApi.Application.Operations.Crud;

/// <summary>
/// Generic operation that deletes an entity by its identifier through <see cref="IRepository"/>.
/// Shared between <c>CrudController</c> and <c>CrudEndpoints</c>.
/// </summary>
/// <typeparam name="TEntity">Entity type being deleted.</typeparam>
public class DeleteEntity<TEntity>(ILogger logger, IRepository repository)
    : OperationBaseWithDefaultResponse<DeleteEntityRequest>(logger)
    where TEntity : EntityBase, new()
{
    /// <inheritdoc />
    protected override Task<OperationResponse> ValidateOperationAsync(DeleteEntityRequest request)
        => OperationResponse.CreateSuccessOk().AsTask();

    /// <inheritdoc />
    protected override async Task<OperationResponse> ProcessOperationAsync(DeleteEntityRequest request)
    {
        try
        {
            await repository.DeleteAsync<TEntity>(request.Id).ConfigureAwait(false);
            return OperationResponse.CreateSuccessOk();
        }
        catch (Exception ex)
        {
            string message = $"Falha ao deletar um(a) {request.ResourceName}.";
            Dictionary<string, object?> logAdditionalData = new() { { "Id", request.Id } };
            Logger.Error(message, ex, logAdditionalData);
            return OperationResponse.CreateInternalServerError(message);
        }
    }
}
