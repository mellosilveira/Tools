using System.Net;

namespace MelloSilveiraTools.Application.Operations.Add;

public record AddResponse : OperationResponseBase<AddResponseData>
{
    public static AddResponse Create() => new();

    public static AddResponse CreateSuccessOk() => CreateSuccessOk<AddResponse>();


    public static AddResponse CreateInternalServerError(string message) => CreateInternalServerError<AddResponse>(message);

    public static AddResponse CreateSuccessCreated(long id) => new()
    {
        Data = new AddResponseData(id),
        StatusCode = HttpStatusCode.Created,
    };

    public static AddResponse CreateConflict(long id, string message) => new()
    {
        Data = new AddResponseData(id),
        ErrorMessages = [message],
        StatusCode = HttpStatusCode.Conflict
    };
}