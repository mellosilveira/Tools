using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Models;
using MelloSilveiraTools.WebApi.ExtensionMethods;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.DataContract;
using MelloSilveiraTools.WebApi.Infrastructure.Services.ApiServiceAgent.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System.IO.Compression;
using System.Text;

namespace UnitTests.WebApi.ExtensionMethods;

public class WebApiExtensionsAndNdjsonTests
{
    private sealed class FakeResponseTrailersFeature : IHttpResponseTrailersFeature
    {
        public IHeaderDictionary Trailers { get; set; } = new HeaderDictionary();
    }

    [Fact]
    public async Task FormFileExtensions_ToCompressedContentAsync_CurrentBehaviorReturnsEmptyBecauseGzipStreamDisposesOnReturn()
    {
        byte[] raw = Encoding.UTF8.GetBytes(new string('A', 256));
        using MemoryStream sourceStream = new(raw);
        FormFile formFile = new(sourceStream, 0, raw.Length, "file", "sample.txt");

        byte[] compressed = await formFile.ToCompressedContentAsync();
        // Currently GZipStream only disposes on method return, leaving only the 10-byte gzip header flushed.
        Assert.Equal(10, compressed.Length);

        using MemoryStream compressedStream = new(compressed);
        await using GZipStream decompress = new(compressedStream, CompressionMode.Decompress);
        using StreamReader reader = new(decompress);
        string restored = await reader.ReadToEndAsync();
        Assert.Equal(string.Empty, restored);
    }

    [Fact]
    public async Task HttpResultExtensions_ProduceExpectedMinimalApiIResults()
    {
        IResult okVoid = await Task.CompletedTask.ToOkResultAsync();
        Assert.IsType<Ok>(okVoid);

        IResult createdVoid = await Task.CompletedTask.ToCreatedResultAsync();
        Assert.IsType<Created>(createdVoid);

        IResult okTyped = await Task.FromResult("payload").ToOkResultAsync();
        Ok<string> okGeneric = Assert.IsType<Ok<string>>(okTyped);
        Assert.Equal("payload", okGeneric.Value);

        IResult createdTyped = await Task.FromResult(42).ToCreatedResultAsync("/items/42");
        Created<int> createdGeneric = Assert.IsType<Created<int>>(createdTyped);
        Assert.Equal("/items/42", createdGeneric.Location);
        Assert.Equal(42, createdGeneric.Value);

        IResult ndjson = await SampleSequence().ToNdjsonResultAsync();
        Assert.IsType<NdjsonResult<string>>(ndjson);

        IResult directNdjson = Results.Extensions.Ndjson(SampleSequence());
        Assert.IsType<NdjsonResult<string>>(directNdjson);
    }

    [Fact]
    public async Task NdjsonResult_ExecuteAsync_WritesItemsAndThrowsNdjsonExceptionOnFailure()
    {
        DefaultHttpContext context = new();
        MemoryStream bodyStream = new();
        context.Response.Body = bodyStream;
        FakeResponseTrailersFeature trailersFeature = new();
        context.Features.Set<IHttpResponseTrailersFeature>(trailersFeature);

        NdjsonResult<string> okResult = new(SampleSequence());
        await okResult.ExecuteAsync(context);

        Assert.Equal(ApplicationConstants.NoSniffHeaderValue, context.Response.Headers[HeaderNames.XContentTypeOptions].ToString());
        Assert.Equal(ApplicationConstants.NdjsonContentType, context.Response.ContentType);
        Assert.Equal(ApplicationConstants.StreamSuccessfullyStatus, trailersFeature.Trailers[ApplicationConstants.StreamStatusTrailerName].ToString());

        bodyStream.Position = 0;
        string text = await new StreamReader(bodyStream).ReadToEndAsync();
        Assert.Equal("\"alpha\"\n\"beta\"\n", text);

        NdjsonResult<string> failingResult = new(FailingSequence());
        NdjsonException thrown = await Assert.ThrowsAsync<NdjsonException>(() => failingResult.ExecuteAsync(new DefaultHttpContext()));
        Assert.IsType<InvalidOperationException>(thrown.InnerException);

        NdjsonException emptyCtor = new();
        NdjsonException customCtor = new("custom", new Exception("inner"));
        Assert.NotNull(emptyCtor);
        Assert.StartsWith("custom", customCtor.Message);
    }

    [Fact]
    public void AsyncResult_And_EndpointSettings_BehaveAsExpected()
    {
        Result failed = Result.CreateBadRequest("Bad input");
        AsyncResult<string> converted = failed;
        Assert.False(converted.Success);
        Assert.Equal(StatusCode.BadRequest, converted.StatusCode);
        Assert.Equal(failed.Messages, converted.Messages);

        converted.Data = SampleSequence();
        Assert.NotNull(converted.Data);

        EndpointSettings endpoint = new() { Uri = "/v1/items", TimeoutInMiliseconds = 2500 };
        Assert.Equal("/v1/items", endpoint.Uri);
        Assert.Equal(2500, endpoint.TimeoutInMiliseconds);
    }

    private static async IAsyncEnumerable<string> SampleSequence()
    {
        await Task.Yield();
        yield return "alpha";
        yield return "beta";
    }

    private static async IAsyncEnumerable<string> FailingSequence()
    {
        await Task.Yield();
        yield return "first";
        throw new InvalidOperationException("stream error");
    }
}
