using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Models;
using MelloSilveiraTools.WebApi.Infrastructure.ResiliencePipelines;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Reflection;
using System.Text;
using UnitTests.WebApi.Fixtures;

namespace UnitTests.WebApi.Infrastructure.Services;

public class ApiServiceAgentBaseTests
{
    private sealed class TestAgentSettings : ApiServiceAgentSettings { }

    public sealed record SampleDto(string Name);

    private sealed class DelegatingStubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responder(request, cancellationToken);
    }

    private sealed class TestApiServiceAgent : ApiServiceAgentBase
    {
        public TestApiServiceAgent(
            ILogger logger,
            ApiServiceAgentSettings settings,
            ApiServiceAgentResiliencePipeline pipeline,
            HttpMessageHandler? customHandler = null)
            : base(logger, settings, pipeline)
        {
            if (customHandler is not null)
            {
                HttpClient.Dispose();
                FieldInfo? backingField = typeof(ApiServiceAgentBase).GetField("<HttpClient>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                backingField!.SetValue(this, new HttpClient(customHandler)
                {
                    BaseAddress = new Uri(settings.BaseAddress),
                    Timeout = TimeSpan.FromSeconds(settings.DefaultTimeoutInSeconds)
                });
            }
        }

        public override string ServiceName => "TestService";

        public Task<ListedResult<SampleDto>> CallGetAsync(string uri, int timeoutMs = 5000)
            => GetAsync<SampleDto>(uri, timeoutMs);

        public IAsyncEnumerable<SampleDto> CallGetStreamAsync(string uri, int timeoutMs = 5000, CancellationToken ct = default)
            => GetStreamAsync<SampleDto>(uri, timeoutMs, nameof(CallGetStreamAsync), ct);

        public Task<Result> CallExecuteAsync(Task<HttpResponseMessage> httpTask, CancellationToken ct = default)
            => ExecuteAsync(httpTask, nameof(CallExecuteAsync), ct);

        public void CallProtectedDispose(bool disposing)
            => Dispose(disposing);
    }

    [Fact]
    public async Task ApiServiceAgentBase_GetAsync_ExecuteAsync_GetStreamAsync_And_Dispose_CoverAllBranches()
    {
        Mock<ILogger<ApiServiceAgentResiliencePipeline>> pipelineLogger = new();
        ApiServiceAgentResiliencePipeline pipeline = new(
            pipelineLogger.Object,
            WebApiTestSettingsFactory.CreateResilienceSettings(),
            _ => ValueTask.FromResult(false));
        Mock<ILogger> agentLogger = new();
        TestAgentSettings settings = new() { BaseAddress = "http://127.0.0.1:5000", DefaultTimeoutInSeconds = 5 };

        using DelegatingStubHandler handler = new((req, ct) =>
        {
            string path = req.RequestUri?.AbsolutePath ?? string.Empty;
            if (path == "/ok")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[{\"name\":\"item1\"}]", Encoding.UTF8, "application/json")
                });
            }

            if (path == "/bad")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("bad request body", Encoding.UTF8, "text/plain")
                });
            }

            if (path == "/null-content")
            {
                HttpResponseMessage msg = new(HttpStatusCode.OK) { Content = null! };
                return Task.FromResult(msg);
            }

            if (path == "/timeout")
            {
                throw new OperationCanceledException(ct);
            }

            if (path == "/boom")
            {
                throw new HttpRequestException("network down");
            }

            if (path == "/stream-ok")
            {
                HttpResponseMessage msg = new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"name\":\"s1\"}\n\nnull\n{\"name\":\"s2\"}\n", Encoding.UTF8, ApplicationConstants.NdjsonContentType)
                };
                msg.TrailingHeaders.Add(ApplicationConstants.StreamStatusTrailerName, ApplicationConstants.StreamSuccessfullyStatus);
                return Task.FromResult(msg);
            }

            if (path == "/stream-missing-trailer")
            {
                HttpResponseMessage msg = new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"name\":\"s1\"}\n", Encoding.UTF8, ApplicationConstants.NdjsonContentType)
                };
                return Task.FromResult(msg);
            }

            if (path == "/stream-bad-trailer")
            {
                HttpResponseMessage msg = new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"name\":\"s1\"}\n", Encoding.UTF8, ApplicationConstants.NdjsonContentType)
                };
                msg.TrailingHeaders.Add(ApplicationConstants.StreamStatusTrailerName, "Faulted");
                return Task.FromResult(msg);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        TestApiServiceAgent agent = new(agentLogger.Object, settings, pipeline, handler);
        Assert.Equal("TestService", agent.ServiceName);
        Assert.Same(pipeline, agent.ResiliencePipeline);

        // 1. GetAsync branches: success, non-success, null content, timeout, exception
        ListedResult<SampleDto> getOk = await agent.CallGetAsync("/ok");
        Assert.True(getOk.Success);
        Assert.Single(getOk.Data!);
        Assert.Equal("item1", getOk.Data![0].Name);

        ListedResult<SampleDto> getBad = await agent.CallGetAsync("/bad");
        Assert.False(getBad.Success);
        Assert.Equal(StatusCode.BadRequest, getBad.StatusCode);

        ListedResult<SampleDto> getNullContent = await agent.CallGetAsync("/null-content");
        Assert.False(getNullContent.Success);
        Assert.Equal(StatusCode.ServiceUnavailable, getNullContent.StatusCode);

        ListedResult<SampleDto> getTimeout = await agent.CallGetAsync("/timeout");
        Assert.False(getTimeout.Success);
        Assert.Equal(StatusCode.RequestTimeout, getTimeout.StatusCode);

        ListedResult<SampleDto> getBoom = await agent.CallGetAsync("/boom");
        Assert.False(getBoom.Success);
        Assert.Equal(StatusCode.ServiceUnavailable, getBoom.StatusCode);

        // 2. ExecuteAsync branches: success, non-success, null content, timeout, exception
        Result execOk = await agent.CallExecuteAsync(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        }));
        Assert.True(execOk.Success);

        Result execBad = await agent.CallExecuteAsync(Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("bad")
        }));
        Assert.False(execBad.Success);
        Assert.Equal(StatusCode.BadRequest, execBad.StatusCode);

        Result execTimeout = await agent.CallExecuteAsync(Task.FromException<HttpResponseMessage>(new OperationCanceledException()));
        Assert.False(execTimeout.Success);
        Assert.Equal(StatusCode.RequestTimeout, execTimeout.StatusCode);

        Result execBoom = await agent.CallExecuteAsync(Task.FromException<HttpResponseMessage>(new InvalidOperationException("err")));
        Assert.False(execBoom.Success);
        Assert.Equal(StatusCode.ServiceUnavailable, execBoom.StatusCode);

        // 3. GetStreamAsync & OpenNdjsonStreamAsync branches
        List<SampleDto> streamOkItems = [];
        await foreach (SampleDto item in agent.CallGetStreamAsync("/stream-ok"))
            streamOkItems.Add(item);
        Assert.Equal(2, streamOkItems.Count);

        List<SampleDto> streamNoTrailerItems = [];
        await foreach (SampleDto item in agent.CallGetStreamAsync("/stream-missing-trailer"))
            streamNoTrailerItems.Add(item);
        Assert.Single(streamNoTrailerItems);

        List<SampleDto> streamBadTrailerItems = [];
        await foreach (SampleDto item in agent.CallGetStreamAsync("/stream-bad-trailer"))
            streamBadTrailerItems.Add(item);
        Assert.Single(streamBadTrailerItems);

        List<SampleDto> streamBadStatus = [];
        await foreach (SampleDto item in agent.CallGetStreamAsync("/bad"))
            streamBadStatus.Add(item);
        Assert.Empty(streamBadStatus);

        List<SampleDto> streamTimeout = [];
        await foreach (SampleDto item in agent.CallGetStreamAsync("/timeout"))
            streamTimeout.Add(item);
        Assert.Empty(streamTimeout);

        List<SampleDto> streamBoom = [];
        await foreach (SampleDto item in agent.CallGetStreamAsync("/boom"))
            streamBoom.Add(item);
        Assert.Empty(streamBoom);

        // 4. Dispose(false), Dispose(), and repeated Dispose()
        agent.CallProtectedDispose(false);
        agent.Dispose();
        agent.Dispose();
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

        int resultAttempts = 0;
        Result retriedResult = await defaultPipeline.ExecuteAsync(_ =>
        {
            resultAttempts++;
            if (resultAttempts == 1)
                return ValueTask.FromResult(Result.CreateServiceUnavailable("transient status"));

            return ValueTask.FromResult(Result.CreateSuccessOk());
        });

        Assert.True(retriedResult.Success);
        Assert.Equal(2, resultAttempts);

        int nonTransientAttempts = 0;
        Result nonTransientResult = await defaultPipeline.ExecuteAsync(_ =>
        {
            nonTransientAttempts++;
            return ValueTask.FromResult(Result.CreateBadRequest("non-transient status"));
        });

        Assert.False(nonTransientResult.Success);
        Assert.Equal(1, nonTransientAttempts);

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
