using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;

namespace MelloSilveiraTools.WebApi.Application.Operations.Crud.Add;

/// <summary>
/// Generic operation that inserts an entity through <see cref="IRepository"/> and returns an <see cref="AddResponse"/>
/// carrying the assigned identifier. Shared between <c>CrudController</c>/<c>CustomControllerBase</c> and the
/// <c>AddEndpoints.MapAdd</c> minimal-API extension so the create path is implemented in exactly one place.
/// </summary>
/// <typeparam name="TEntity">Entity type being persisted.</typeparam>
public class AddEntity<TEntity>(ILogger logger, IRepository repository)
    : OperationBase<AddEntityRequest<TEntity>, AddResponse>(logger)
    where TEntity : EntityBase, new()
{
    /// <inheritdoc />
    protected override Task<AddResponse> ValidateOperationAsync(AddEntityRequest<TEntity> request)
        => OperationResponse.CreateSuccessOk<AddResponse>().AsTask();

    /// <inheritdoc />
    protected override async Task<AddResponse> ProcessOperationAsync(AddEntityRequest<TEntity> request)
    {
        try
        {
            long id = await repository.InsertAsync(request.Entity).ConfigureAwait(false);
            return AddResponse.CreateSuccessCreated(id);
        }
        catch (Exception ex)
        {
            string message = $"Falha ao adicionar um(a) {request.ResourceName}.";

            Dictionary<string, object?> logAdditionalData = new() { { "Entity", request.Entity } };
            Logger.Error(message, ex, logAdditionalData);

            return OperationResponse.CreateInternalServerError(message);
        }
    }
}
