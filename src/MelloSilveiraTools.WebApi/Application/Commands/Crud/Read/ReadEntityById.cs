using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;

namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;

/// <summary>
/// Generic operation that loads a single entity by its identifier through <see cref="IRepository"/>.
/// Shared between <c>CrudController</c> and <c>CrudEndpoints</c>.
/// </summary>
/// <typeparam name="TEntity">Entity type being read.</typeparam>
public class ReadEntityById<TEntity>(IRepository repository) : CommandBaseWithData<ReadEntityByIdRequest, TEntity> where TEntity : EntityBase, new()
{
    /// <inheritdoc />
    protected override async Task<Result<TEntity>> ExecuteCommandAsync(ReadEntityByIdRequest request)
    {
        TEntity? entity = await repository.GetAsync<TEntity>(request.Id).ConfigureAwait(false);
        return entity is null ? Result.CreateNotFound() : Result.CreateSuccessOk(entity);
    }
}
