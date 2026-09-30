using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Application.Commands;

/// <summary>
/// Represents the base for all commands in the application.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the command.</typeparam>
/// <typeparam name="TResult">Response type produced by the command.</typeparam>
/// <param name="validator"></param>
public abstract class CommandBase<TRequest, TResult>(IValidator<TRequest>? validator = null)
    where TRequest : class
    where TResult : ResultBase, new()
{
    public IValidator<TRequest>? Validator { get; } = validator;

    /// <summary>
    /// The main method of all commands.
    /// Asynchronously, orchestrates and validates the commands.
    /// </summary>
    /// <param name="request">The command request content.</param>
    /// <returns>The command response.</returns>
    /// <example>
    /// <code>
    /// var command = serviceProvider.GetRequiredService&lt;CreateUserCommand&gt;();
    /// var response = await command.ExecuteAsync(new CreateUserRequest { Email = "user@example.com" });
    /// if (!response.Success)
    ///     return BadRequest(response.ErrorMessages);
    /// return StatusCode((int)response.StatusCode);
    /// </code>
    /// </example>
    public Task<TResult> ExecuteAsync(TRequest request)
    {
        var result = Validator?.Validate(request);
        return result is null || result.Success
            ? ExecuteCommandAsync(request)
            : Task.FromResult(Result.Create<TResult>(result));
    }

    /// <summary>
    /// Asynchronously executes the use-case domain logic for this command. Implementations should
    /// rely on <see cref="ILogger"/> (inherited from this base class) for diagnostic logging and
    /// build their response using the <see cref="Result"/> factory helpers
    /// (<c>CreateSuccessOk</c>, <c>CreateNotFound</c>, <c>CreateUnknownError</c>, etc.) for
    /// expected outcomes. Unexpected exceptions should be allowed to propagate — they are caught by
    /// <see cref="ExecuteAsync"/> and translated into a 500 Internal Server Error response.
    /// </summary>
    /// <param name="request">The request payload.</param>
    /// <returns>The command response, populated with the outcome of the domain logic.</returns>
    protected abstract Task<TResult> ExecuteCommandAsync(TRequest request);
}

/// <summary>
/// Base class for commands that return an <see cref="Result{TResponseData}"/> carrying a single data payload.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the command.</typeparam>
/// <typeparam name="TResponseData">Type of the data payload returned by the command.</typeparam>
public abstract class CommandBaseWithData<TRequest, TResponseData>(IValidator<TRequest>? validator = null) : CommandBase<TRequest, Result<TResponseData>>(validator)
    where TRequest : class
    where TResponseData : class
{ }

/// <summary>
/// Base class for commands that return a list of items through <see cref="ListedResult{TResponseData}"/>.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the command.</typeparam>
/// <typeparam name="TResponseData">Type of each item returned by the command.</typeparam>
public abstract class ListedCommandBase<TRequest, TResponseData>(IValidator<TRequest>? validator = null) : CommandBase<TRequest, ListedResult<TResponseData>>(validator)
    where TRequest : class
    where TResponseData : class
{ }

/// <summary>
/// Base class for commands that return a paginated list of items through <see cref="PagedResult{TResponseData}"/>.
/// </summary>
/// <typeparam name="TRequest">Request type consumed by the command.</typeparam>
/// <typeparam name="TResponseData">Type of each item returned in the page.</typeparam>
public abstract class PagedCommandBase<TRequest, TResponseData>(IValidator<TRequest>? validator = null) : CommandBase<TRequest, PagedResult<TResponseData>>(validator)
    where TRequest : class
    where TResponseData : class
{ }

/// <summary>
/// Represents the base for all commands that uses the default response (<see cref="Result"/>).
/// </summary>
public abstract class CommandBaseWithDefaultResponse<TRequest>(IValidator<TRequest>? validator = null) : CommandBase<TRequest, Result>(validator) where TRequest : class;

/// <summary>
/// Represents the base for all commands that does not use a request.
/// </summary>
public abstract class CommandBaseWithoutRequest<TResponseData> where TResponseData : class
{
    /// <summary>
    /// The main method of all commands.
    /// Asynchronously, orchestrates and validates the commands.
    /// </summary>
    /// <returns>The command response.</returns>
    public async Task<Result<TResponseData>> ExecuteAsync() => await ExecuteCommandAsync().ConfigureAwait(false);

    /// <summary>
    /// Asynchronously executes the use-case domain logic for this request-less command.
    /// Implementations should use <see cref="ILogger"/> for diagnostics and the
    /// <see cref="Result"/> factory helpers to build expected outcomes. Unexpected
    /// exceptions should propagate — they are caught by <see cref="ExecuteAsync"/> and translated
    /// into a 500 Internal Server Error response.
    /// </summary>
    /// <returns>The command response carrying the outcome of the domain logic.</returns>
    protected abstract Task<Result<TResponseData>> ExecuteCommandAsync();
}

/// <summary>
/// Represents the base for all commands that does not use a request.
/// </summary>
public abstract class DefaultCommandBase
{
    /// <summary>
    /// The main method of all commands.
    /// Asynchronously, orchestrates and validates the commands.
    /// </summary>
    /// <returns>The command response.</returns>
    public async Task<Result> ExecuteAsync() => await ExecuteCommandAsync().ConfigureAwait(false);

    /// <summary>
    /// Asynchronously executes the use-case domain logic for this request-less command.
    /// Implementations should use <see cref="ILogger"/> for diagnostics and the
    /// <see cref="Result"/> factory helpers to build expected outcomes. Unexpected
    /// exceptions should propagate — they are caught by <see cref="ExecuteAsync"/> and translated
    /// into a 500 Internal Server Error response.
    /// </summary>
    /// <returns>The command response carrying the outcome of the domain logic.</returns>
    protected abstract Task<Result> ExecuteCommandAsync();
}
