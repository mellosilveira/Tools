using Amazon.S3;
using Azure;
using MelloSilveiraTools.Core.ResiliencePipelines;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using System.Net;
using System.Net.Mail;

namespace UnitTests.Core.ResiliencePipelines;

/// <summary>
/// Unit tests for <see cref="DefaultResiliencePipeline"/>, <see cref="AwsS3ResiliencePipeline"/>,
/// <see cref="BlobStorageResiliencePipeline"/>, and <see cref="SmtpResiliencePipeline"/>
/// verifying transient retry execution, fast-failure on permanent errors, and delegate overloads.
/// </summary>
public sealed class ResiliencePipelineTests
{
    private static readonly ResiliencePipelineSettings FastSettings = new()
    {
        BackoffType = DelayBackoffType.Constant,
        MaxRetryAttempts = 2,
        DelayInMilliseconds = 1,
        UseJitter = false
    };

    [Fact]
    public async Task DefaultResiliencePipeline_RetriesWhenPredicateMatchesAndSucceeds()
    {
        DefaultResiliencePipeline pipeline = new(
            NullLogger<DefaultResiliencePipeline>.Instance,
            FastSettings,
            new PredicateBuilder().Handle<InvalidOperationException>());

        int executionCount = 0;

        string result = await pipeline.ExecuteAsync(async (ResilienceContext _) =>
        {
            await Task.Yield();
            executionCount++;
            if (executionCount == 1)
            {
                throw new InvalidOperationException("Transient failure");
            }

            return "Success";
        });

        Assert.Equal(2, executionCount);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task DefaultResiliencePipeline_FailsFastWhenPredicateDoesNotMatch()
    {
        DefaultResiliencePipeline pipeline = new(
            NullLogger<DefaultResiliencePipeline>.Instance,
            FastSettings,
            new PredicateBuilder().Handle<InvalidOperationException>());

        int executionCount = 0;

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await pipeline.ExecuteAsync(async (ResilienceContext _) =>
            {
                await Task.Yield();
                executionCount++;
                throw new ArgumentException("Permanent failure");
            });
        });

        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task AwsS3ResiliencePipeline_RetriesOnTransientS3Exception()
    {
        AwsS3ResiliencePipeline pipeline = new(
            NullLogger<AwsS3ResiliencePipeline>.Instance,
            FastSettings);

        int executionCount = 0;

        int result = await pipeline.ExecuteAsync(async (ResilienceContext _) =>
        {
            await Task.Yield();
            executionCount++;
            if (executionCount == 1)
            {
                throw new AmazonS3Exception("Service Unavailable") { StatusCode = HttpStatusCode.ServiceUnavailable };
            }

            return 42;
        });

        Assert.Equal(2, executionCount);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task AwsS3ResiliencePipeline_FailsFastOnNonTransientS3Exception()
    {
        AwsS3ResiliencePipeline pipeline = new(
            NullLogger<AwsS3ResiliencePipeline>.Instance,
            FastSettings);

        int executionCount = 0;

        await Assert.ThrowsAsync<AmazonS3Exception>(async () =>
        {
            await pipeline.ExecuteAsync(async (ResilienceContext _) =>
            {
                await Task.Yield();
                executionCount++;
                throw new AmazonS3Exception("NoSuchKey") { StatusCode = HttpStatusCode.NotFound };
            });
        });

        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task BlobStorageResiliencePipeline_RetriesOnTransientAzureException()
    {
        BlobStorageResiliencePipeline pipeline = new(
            NullLogger<BlobStorageResiliencePipeline>.Instance,
            FastSettings);

        int executionCount = 0;

        string result = await pipeline.ExecuteAsync(async (ResilienceContext _) =>
        {
            await Task.Yield();
            executionCount++;
            if (executionCount == 1)
            {
                throw new RequestFailedException((int)HttpStatusCode.ServiceUnavailable, "Server Busy");
            }

            return "AzureSuccess";
        });

        Assert.Equal(2, executionCount);
        Assert.Equal("AzureSuccess", result);
    }

    [Fact]
    public async Task BlobStorageResiliencePipeline_FailsFastOnNonTransientAzureException()
    {
        BlobStorageResiliencePipeline pipeline = new(
            NullLogger<BlobStorageResiliencePipeline>.Instance,
            FastSettings);

        int executionCount = 0;

        await Assert.ThrowsAsync<RequestFailedException>(async () =>
        {
            await pipeline.ExecuteAsync(async (ResilienceContext _) =>
            {
                await Task.Yield();
                executionCount++;
                throw new RequestFailedException((int)HttpStatusCode.NotFound, "BlobNotFound");
            });
        });

        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task SmtpResiliencePipeline_RetriesOnSmtpException()
    {
        SmtpResiliencePipeline pipeline = new(
            NullLogger<SmtpResiliencePipeline>.Instance,
            FastSettings);

        int executionCount = 0;

        bool result = await pipeline.ExecuteAsync(async (ResilienceContext _) =>
        {
            await Task.Yield();
            executionCount++;
            if (executionCount == 1)
            {
                throw new SmtpException("SMTP timeout");
            }

            return true;
        });

        Assert.Equal(2, executionCount);
        Assert.True(result);
    }

    [Fact]
    public async Task DefaultResiliencePipeline_ExecutesVoidTaskOverloadSuccessfully()
    {
        DefaultResiliencePipeline pipeline = new(
            NullLogger<DefaultResiliencePipeline>.Instance,
            FastSettings,
            new PredicateBuilder().Handle<InvalidOperationException>());

        bool executed = false;

        await pipeline.ExecuteAsync(async (ResilienceContext _) =>
        {
            await Task.Yield();
            executed = true;
        });

        Assert.True(executed);
    }
}
