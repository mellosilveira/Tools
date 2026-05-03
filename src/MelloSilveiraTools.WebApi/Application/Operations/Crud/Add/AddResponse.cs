using System.Net;

namespace MelloSilveiraTools.WebApi.Application.Operations.Crud.Add;

/// <summary>
/// Response produced by add/create operations, optionally exposing the identifier of the newly created resource.
/// </summary>
public record AddResponse : OperationResponse<AddResponseData>
{
    /// <summary>
    /// Creates a successful 201 Created response that returns the identifier assigned to the new resource.
    /// </summary>
    /// <param name="id">Identifier of the newly created resource.</param>
    public static AddResponse CreateSuccessCreated(long id) => new()
    {
        Data = new AddResponseData(id),
        StatusCode = HttpStatusCode.Created,
        Success = true,
    };

    /// <summary>
    /// Creates a 409 Conflict response that also exposes the identifier of the conflicting resource.
    /// </summary>
    /// <param name="id">Identifier of the existing resource that conflicts with the request.</param>
    /// <param name="message">Error message describing the conflict.</param>
    public static AddResponse CreateConflict(long id, string message) => new()
    {
        Data = new AddResponseData(id),
        Messages = [message],
        StatusCode = HttpStatusCode.Conflict,
    };

    public static implicit operator AddResponse(OperationResponse response) => new() { Messages = response.Messages, StatusCode = response.StatusCode, Success = response.Success };
}
