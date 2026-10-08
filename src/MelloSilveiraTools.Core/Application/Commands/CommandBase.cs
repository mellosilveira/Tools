using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.Core.Application.Commands;

/// <summary>
/// Foundational structure for orchestrating business operations within the application.
/// Enforces upfront validation of user and integration requests to safeguard business invariants,
/// delivering consistent, reliable outcomes across all operations.
/// </summary>
/// <typeparam name="TRequest">The input parameters required for the operation.</typeparam>
/// <typeparam name="TResult">The business result produced by the operation.</typeparam>
/// <param name="validator">Optional validator that inspects input data integrity prior to execution.</param>
public abstract class CommandBase<TRequest, TResult>(IValidator<TRequest>? validator = null)
    where TRequest : class
    where TResult : ResultBase, new()
{
    /// <summary>
    /// Gets the validation rules assigned to inspect the incoming business request.
    /// </summary>
    public IValidator<TRequest>? Validator { get; } = validator;

    /// <summary>
    /// Orchestrates execution: verifies input validity and executes domain logic if checks succeed.
    /// If validation issues are found, stops processing immediately and returns corrective guidance.
    /// </summary>
    /// <param name="request">Input data for the business operation.</param>
    /// <returns>A structured business outcome confirming success or detailing validation points.</returns>
    public Task<TResult> ExecuteAsync(TRequest request)
    {
        Result? result = Validator?.Validate(request);
        return result is null || result.Success
            ? ExecuteCommandAsync(request)
            : Task.FromResult(Result.Create<TResult>(result));
    }

    /// <summary>
    /// Executes core domain business logic after request validation passes.
    /// </summary>
    /// <param name="request">Validated input data.</param>
    /// <returns>The result of the business execution.</returns>
    protected abstract Task<TResult> ExecuteCommandAsync(TRequest request);
}

/// <summary>
/// Business command that produces a single structured outcome payload upon completion.
/// </summary>
/// <typeparam name="TRequest">Input request type.</typeparam>
/// <typeparam name="TResponseData">Type of the payload returned upon success.</typeparam>
/// <param name="validator">Optional request validator.</param>
public abstract class CommandBaseWithData<TRequest, TResponseData>(IValidator<TRequest>? validator = null) : CommandBase<TRequest, Result<TResponseData>>(validator)
    where TRequest : class
    where TResponseData : class
{ }

/// <summary>
/// Business command that returns a list of items matching the query or filter criteria.
/// </summary>
/// <typeparam name="TRequest">Filter or query parameter type.</typeparam>
/// <typeparam name="TResponseData">Type of each item returned in the list.</typeparam>
/// <param name="validator">Optional request validator.</param>
public abstract class ListedCommandBase<TRequest, TResponseData>(IValidator<TRequest>? validator = null) : CommandBase<TRequest, ListedResult<TResponseData>>(validator)
    where TRequest : class
    where TResponseData : class
{ }

/// <summary>
/// Business command that returns a paginated dataset optimized for UI tables and high-volume reporting.
/// </summary>
/// <typeparam name="TRequest">Paging and search criteria type.</typeparam>
/// <typeparam name="TResponseData">Type of each item within the returned page.</typeparam>
/// <param name="validator">Optional request validator.</param>
public abstract class PagedCommandBase<TRequest, TResponseData>(IValidator<TRequest>? validator = null) : CommandBase<TRequest, PagedResult<TResponseData>>(validator)
    where TRequest : class
    where TResponseData : class
{ }

/// <summary>
/// Business command that carries out an action and confirms completion via a standard status result.
/// </summary>
/// <typeparam name="TRequest">Input request type.</typeparam>
/// <param name="validator">Optional request validator.</param>
public abstract class CommandBaseWithDefaultResponse<TRequest>(IValidator<TRequest>? validator = null) : CommandBase<TRequest, Result>(validator) where TRequest : class;

/// <summary>
/// Business command triggered without incoming parameters that returns structured output upon completion.
/// </summary>
/// <typeparam name="TResponseData">Type of the payload produced by the operation.</typeparam>
public abstract class CommandBaseWithoutRequest<TResponseData> where TResponseData : class
{
    /// <summary>
    /// Executes the parameterless business operation.
    /// </summary>
    /// <returns>A structured business outcome containing output data.</returns>
    public async Task<Result<TResponseData>> ExecuteAsync() => await ExecuteCommandAsync().ConfigureAwait(false);

    /// <summary>
    /// Executes the domain logic for this parameterless operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    protected abstract Task<Result<TResponseData>> ExecuteCommandAsync();
}

/// <summary>
/// Business command triggered without incoming parameters that returns standard completion confirmation.
/// </summary>
public abstract class DefaultCommandBase
{
    /// <summary>
    /// Executes the parameterless business operation.
    /// </summary>
    /// <returns>Standard completion confirmation.</returns>
    public async Task<Result> ExecuteAsync() => await ExecuteCommandAsync().ConfigureAwait(false);

    /// <summary>
    /// Executes the domain logic for this parameterless operation.
    /// </summary>
    /// <returns>Standard completion confirmation.</returns>
    protected abstract Task<Result> ExecuteCommandAsync();
}
