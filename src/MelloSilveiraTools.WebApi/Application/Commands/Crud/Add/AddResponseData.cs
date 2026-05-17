namespace MelloSilveiraTools.WebApi.Application.Commands.Crud.Add;

/// <summary>
/// Data payload returned by add/create operations, carrying the identifier of the new resource.
/// </summary>
/// <param name="Id">Identifier assigned to the newly created resource.</param>
public record AddResponseData(long Id);
