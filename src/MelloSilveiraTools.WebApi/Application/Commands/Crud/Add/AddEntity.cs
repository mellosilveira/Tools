using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;

namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Add;

/// <summary>
/// Generic operation that inserts an entity through <see cref="IRepository"/> and returns an <see cref="AddResponse"/>
/// carrying the assigned identifier. Shared between <c>CrudController</c>/<c>CustomControllerBase</c> and the
/// <c>AddEndpoints.MapAdd</c> minimal-API extension so the create path is implemented in exactly one place.
/// </summary>
/// <typeparam name="TEntity">Entity type being persisted.</typeparam>
public class AddEntity<TEntity>(IRepository repository)
    : CommandBase<AddEntityRequest<TEntity>, AddResponse>
    where TEntity : EntityBase, new()
{
    /// <inheritdoc />
    protected override async Task<AddResponse> ExecuteCommandAsync(AddEntityRequest<TEntity> request)
    {
        long id = await repository.InsertAsync(request.Entity).ConfigureAwait(false);
        return AddResponse.CreateSuccessCreated(id);
    }
}
