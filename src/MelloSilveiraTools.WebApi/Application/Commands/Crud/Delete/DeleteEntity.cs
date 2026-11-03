using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;

namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Delete;

/// <summary>
/// Generic operation that deletes an entity by its identifier through <see cref="IRepository"/>.
/// Shared between <c>CrudController</c> and <c>CrudEndpoints</c>.
/// </summary>
/// <typeparam name="TEntity">Entity type being deleted.</typeparam>
public class DeleteEntity<TEntity>(IRepository repository) : CommandBaseWithDefaultResponse<DeleteEntityRequest> where TEntity : EntityBase, new()
{
    /// <inheritdoc />
    protected override async Task<Result> ExecuteCommandAsync(DeleteEntityRequest request, CancellationToken cancellationToken = default)
    {
        await repository.DeleteAsync<TEntity>(request.Id, cancellationToken).ConfigureAwait(false);
        return Result.CreateSuccessOk();
    }
}
