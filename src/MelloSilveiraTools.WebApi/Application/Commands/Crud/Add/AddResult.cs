using MelloSilveiraTools.Core.Models;

namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Add;

/// <summary>
/// Response produced by add/create operations, optionally exposing the identifier of the newly created resource.
/// </summary>
public record AddResult : Result<AddResultData>
{
    /// <summary>
    /// Creates a successful 201 Created response that returns the identifier assigned to the new resource.
    /// </summary>
    /// <param name="id">Identifier of the newly created resource.</param>
    public static AddResult CreateSuccessCreated(long id) => new()
    {
        Data = new AddResultData(id),
        StatusCode = StatusCode.Created,
        Success = true,
    };

    /// <summary>
    /// Creates a 409 Conflict response that also exposes the identifier of the conflicting resource.
    /// </summary>
    /// <param name="id">Identifier of the existing resource that conflicts with the request.</param>
    /// <param name="message">Error message describing the conflict.</param>
    public static AddResult CreateConflict(long id, string message) => new()
    {
        Data = new AddResultData(id),
        Messages = [message],
        StatusCode = StatusCode.Conflict,
    };

    public static implicit operator AddResult(Result response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}
