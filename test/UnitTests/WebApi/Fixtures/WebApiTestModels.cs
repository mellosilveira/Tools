using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.WebApi.Application.Controllers;
using Microsoft.Extensions.Logging;
using Polly;

namespace UnitTests.WebApi.Fixtures;

public sealed record WebApiTestEntity : EntityBase
{
    public string Name { get; init; } = string.Empty;
}

public sealed record WebApiTestFilter : FilterBase
{
    public string? Name { get; init; }
}

public sealed class WebApiTestController(ILogger<WebApiTestController> logger)
    : CrudController<WebApiTestEntity, WebApiTestFilter>(logger)
{
    protected override string ResourceName => "web api test entity";
}

public static class WebApiTestSettingsFactory
{
    public static ResiliencePipelineSettings CreateResilienceSettings() => new()
    {
        BackoffType = DelayBackoffType.Constant,
        DelayInMilliseconds = 1,
        MaxRetryAttempts = 1,
        UseJitter = false
    };
}
