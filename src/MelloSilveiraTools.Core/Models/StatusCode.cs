namespace MelloSilveiraTools.Core.Models;

public enum StatusCode : short
{
    OK = 200,
    Created = 201,
    NoContent = 204,
    BadRequest = 400,
    Unauthorized = 401,
    NotFound = 404,
    RequestTimeout = 408,
    Conflict = 409,
    UnprocessableEntity = 422,
    UnknownError = 500,
    ServiceUnavailable = 503,
}