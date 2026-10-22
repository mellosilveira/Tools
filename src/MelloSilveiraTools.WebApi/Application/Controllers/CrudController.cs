using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Add;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Update;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.WebApi.Application.Controllers;

/// <summary>
/// Generic controller exposing standard CRUD and streaming endpoints for an entity and its filter.
/// All HTTP-agnostic logic lives in the <c>MelloSilveiraTools.WebApi.Application.commands.Crud</c> commands,
/// which are also consumed by the equivalent minimal-API extensions in <see cref="Endpoints.CrudEndpoints"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type handled by the controller.</typeparam>
/// <typeparam name="TFilter">The filter type used to query the entity.</typeparam>
/// <param name="logger">Logger used to record failures raised while handling CRUD requests.</param>
public abstract class CrudController<TEntity, TFilter>(ILogger logger) : CustomControllerBase(logger)
    where TEntity : EntityBase, new()
    where TFilter : FilterBase, new()
{
    /// <summary>
    /// Human-readable name of the resource, used when building log and error messages.
    /// </summary>
    protected abstract string ResourceName { get; }

    /// <summary>
    /// Persists a new entity.
    /// </summary>
    /// <param name="command">command that performs the insert.</param>
    /// <param name="entity">Entity instance to be persisted.</param>
    /// <param name="cancellationToken"></param>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost]
    public Task<ActionResult<AddResult>> Add(
        [FromServices] AddEntity<TEntity> command,
        [FromBody] TEntity entity,
        CancellationToken cancellationToken = default)
        => Add(command, entity, ResourceName, cancellationToken);

    /// <summary>
    /// Retrieves a single entity by its identifier.
    /// </summary>
    /// <param name="command">command that loads the entity.</param>
    /// <param name="id">Identifier of the entity to be returned.</param>
    /// <param name="cancellationToken"></param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<Result<TEntity>>> Read(
        [FromServices] ReadEntityById<TEntity> command,
        [FromRoute] long id,
        CancellationToken cancellationToken = default)
        => await command
            .ExecuteAsync(new ReadEntityByIdRequest { Id = id, ResourceName = ResourceName }, cancellationToken)
            .BuildHttpResponseAsync()
            .ConfigureAwait(false);

    /// <summary>
    /// Retrieves a paginated list of entities that match the supplied filter.
    /// </summary>
    /// <param name="command">command that queries the entities.</param>
    /// <param name="filter">Filter criteria applied to the query.</param>
    /// <param name="pagination">Pagination parameters (offset and limit).</param>
    /// <param name="cancellationToken"></param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<TEntity>>> Read(
        [FromServices] ReadEntityPaged<TEntity, TFilter> command,
        [FromQuery] TFilter filter,
        [FromQuery] Pagination pagination,
        CancellationToken cancellationToken = default)
        => await command
            .ExecuteAsync(new ReadEntityPagedRequest<TFilter>
            {
                Filter = filter,
                Pagination = pagination,
                ResourceName = ResourceName
            }, cancellationToken)
            .BuildHttpResponseAsync()
            .ConfigureAwait(false);

    /// <summary>
    /// Updates an existing entity identified by the route parameter.
    /// </summary>
    /// <param name="command">command that persists the changes.</param>
    /// <param name="id">Identifier of the entity to be updated.</param>
    /// <param name="entity">Updated entity payload.</param>
    /// <param name="cancellationToken"></param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<Result>> Update(
        [FromServices] UpdateEntity<TEntity> command,
        [FromRoute] long id,
        [FromBody] TEntity entity,
        CancellationToken cancellationToken = default)
        => await command
            .ExecuteAsync(new UpdateEntityRequest<TEntity> { Id = id, Entity = entity, ResourceName = ResourceName }, cancellationToken)
            .BuildHttpResponseAsync()
            .ConfigureAwait(false);

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="command">command that deletes the entity.</param>
    /// <param name="id">Identifier of the entity to be deleted.</param>
    /// <param name="cancellationToken"></param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<Result>> Delete(
        [FromServices] DeleteEntity<TEntity> command,
        [FromRoute] long id,
        CancellationToken cancellationToken = default)
        => await command
            .ExecuteAsync(new DeleteEntityRequest { Id = id, ResourceName = ResourceName }, cancellationToken)
            .BuildHttpResponseAsync()
            .ConfigureAwait(false);

    /// <summary>
    /// Streams entities that match the supplied filter as newline-delimited JSON (NDJSON).
    /// </summary>
    /// <param name="repository">Repository used to source the entities.</param>
    /// <param name="filter">Filter criteria applied to the query.</param>
    /// <param name="cancellationToken"></param>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("stream")]
    public async Task Stream(
        [FromServices] IRepository repository,
        [FromQuery] TFilter filter,
        CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<TEntity> entities = repository.GetAsync<TEntity, TFilter>(filter, pagination: null, cancellationToken);
        await Stream(entities, ResourceName, cancellationToken).ConfigureAwait(false);
    }
}
