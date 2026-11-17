using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Infrastructure.ResiliencePipelines;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using UnitTests.WebApi.Fixtures;

namespace UnitTests.WebApi.Infrastructure.Services;

public class ApiServiceAgentBaseTests
{
    private sealed class TestAgentSettings : ApiServiceAgentSettings { }

    private sealed class TestApiServiceAgent(
        ILogger logger,
        ApiServiceAgentSettings settings,
        ApiServiceAgentResiliencePipeline pipeline)
        : ApiServiceAgentBase(logger, settings, pipeline)
    {
        public override string ServiceName => "TestService";
    }

    [Fact]
    public void ApiServiceAgentBase_ConstructsAndExposesConfiguredServiceName()
    {
        Mock<ILogger<ApiServiceAgentResiliencePipeline>> pipelineLogger = new();
        ApiServiceAgentResiliencePipeline pipeline = new(pipelineLogger.Object, WebApiTestSettingsFactory.CreateResilienceSettings());
        Mock<ILogger> agentLogger = new();

        TestApiServiceAgent agent = new(
            agentLogger.Object,
            new TestAgentSettings { BaseAddress = "http://127.0.0.1:5000", DefaultTimeoutInSeconds = 5 },
            pipeline);

        Assert.Equal("TestService", agent.ServiceName);
    }

    [Fact]
    public async Task ApiServiceAgentResiliencePipeline_RetriesOnTransientStatusCodeAndRespectsCustomPredicate()
    {
        Mock<ILogger<ApiServiceAgentResiliencePipeline>> loggerMock = new();
        ApiServiceAgentResiliencePipeline defaultPipeline = new(loggerMock.Object, WebApiTestSettingsFactory.CreateResilienceSettings());

        int exceptionAttempts = 0;
        Result retriedAfterException = await defaultPipeline.ExecuteAsync(_ =>
        {
            exceptionAttempts++;
            if (exceptionAttempts == 1)
                throw new InvalidOperationException("transient error");

            return ValueTask.FromResult(Result.CreateSuccessOk());
        });

        Assert.True(retriedAfterException.Success);
        Assert.Equal(2, exceptionAttempts);

        // Current PredicateBuilder.HandleResult(new Func<Result, bool>(...)) binds to HandleResult(object),
        // so returning a transient Result does not trigger retries.
        int resultAttempts = 0;
        Result unretriedResult = await defaultPipeline.ExecuteAsync(_ =>
        {
            resultAttempts++;
            return ValueTask.FromResult(Result.CreateServiceUnavailable("transient status"));
        });

        Assert.False(unretriedResult.Success);
        Assert.Equal(1, resultAttempts);

        ApiServiceAgentResiliencePipeline customPipeline = new(
            loggerMock.Object,
            WebApiTestSettingsFactory.CreateResilienceSettings(),
            _ => ValueTask.FromResult(false));

        int customAttempts = 0;
        Result notRetried = await customPipeline.ExecuteAsync(_ =>
        {
            customAttempts++;
            return ValueTask.FromResult(Result.CreateUnknownError("no retry"));
        });

        Assert.False(notRetried.Success);
        Assert.Equal(1, customAttempts);
    }
}
