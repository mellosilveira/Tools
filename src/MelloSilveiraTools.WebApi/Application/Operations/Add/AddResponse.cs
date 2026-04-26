using System.Net;

namespace MelloSilveiraTools.WebApi.Application.Operations.Add;

/// <summary>
/// Response produced by add/create operations, optionally exposing the identifier of the newly created resource.
/// </summary>
public record AddResponse : OperationResponseBase<AddResponseData>
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
        ErrorMessages = [message],
        StatusCode = HttpStatusCode.Conflict,
    };
}
