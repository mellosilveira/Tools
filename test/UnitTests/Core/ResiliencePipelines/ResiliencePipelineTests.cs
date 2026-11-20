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

    [Fact]
    public async Task DefaultResiliencePipeline_ExecuteAsyncWithSyncAndAsyncFallbacks_ReturnsFallbackOnFailureAndPrimaryOnSuccess()
    {
        ResiliencePipelineSettings fastSingleRetrySettings = new()
        {
            BackoffType = DelayBackoffType.Constant,
            MaxRetryAttempts = 1,
            DelayInMilliseconds = 1,
            UseJitter = false
        };

        DefaultResiliencePipeline valuePipeline = new(
            NullLogger<DefaultResiliencePipeline>.Instance,
            fastSingleRetrySettings,
            new PredicateBuilder().Handle<InvalidOperationException>());

        int syncSuccess = await valuePipeline.ExecuteAsync(
            static _ => ValueTask.FromResult(10),
            static _ => -1);

        int syncFallback = await valuePipeline.ExecuteAsync<int>(
            static _ => ValueTask.FromException<int>(new InvalidOperationException("boom")),
            static ex => ex is InvalidOperationException ? 99 : 0);

        string asyncSuccess = await valuePipeline.ExecuteAsync(
            static _ => ValueTask.FromResult("ok"),
            static _ => ValueTask.FromResult("fallback"));

        string asyncFallback = await valuePipeline.ExecuteAsync<string>(
            static _ => ValueTask.FromException<string>(new InvalidOperationException("boom")),
            static ex => ValueTask.FromResult($"recovered:{ex.Message}"));

        // Separate instance so cumulative failure count never hits the default CircuitBreaker threshold.
        DefaultResiliencePipeline voidPipeline = new(
            NullLogger<DefaultResiliencePipeline>.Instance,
            fastSingleRetrySettings,
            new PredicateBuilder().Handle<InvalidOperationException>());

        bool voidSyncFallbackCalled = false;
        await voidPipeline.ExecuteAsync(
            static _ => ValueTask.CompletedTask,
            _ => voidSyncFallbackCalled = true);
        Assert.False(voidSyncFallbackCalled);

        await voidPipeline.ExecuteAsync(
            static _ => ValueTask.FromException(new InvalidOperationException("void-boom")),
            ex => voidSyncFallbackCalled = ex is InvalidOperationException);
        Assert.True(voidSyncFallbackCalled);

        bool voidAsyncFallbackCalled = false;
        await voidPipeline.ExecuteAsync(
            static _ => ValueTask.CompletedTask,
            _ =>
            {
                voidAsyncFallbackCalled = true;
                return ValueTask.CompletedTask;
            });
        Assert.False(voidAsyncFallbackCalled);

        await voidPipeline.ExecuteAsync(
            static _ => ValueTask.FromException(new InvalidOperationException("void-async-boom")),
            ex =>
            {
                voidAsyncFallbackCalled = ex is InvalidOperationException;
                return ValueTask.CompletedTask;
            });
        Assert.True(voidAsyncFallbackCalled);

        Assert.Equal(10, syncSuccess);
        Assert.Equal(99, syncFallback);
        Assert.Equal("ok", asyncSuccess);
        Assert.Equal("recovered:boom", asyncFallback);
    }

    [Fact]
    public async Task DefaultResiliencePipeline_DefaultCircuitBreaker_OpensAfterMinimumThroughputFailures()
    {
        ResiliencePipelineSettings fastSingleRetrySettings = new()
        {
            BackoffType = DelayBackoffType.Constant,
            MaxRetryAttempts = 1,
            DelayInMilliseconds = 1,
            UseJitter = false
        };

        DefaultResiliencePipeline pipeline = new(
            NullLogger<DefaultResiliencePipeline>.Instance,
            fastSingleRetrySettings,
            new PredicateBuilder().Handle<InvalidOperationException>());

        for (int i = 0; i < 6; i++)
        {
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await pipeline.ExecuteAsync<int>(static _ => ValueTask.FromException<int>(new InvalidOperationException("fail"))));
        }

        await Assert.ThrowsAsync<Polly.CircuitBreaker.BrokenCircuitException>(async () =>
            await pipeline.ExecuteAsync(static _ => ValueTask.FromResult(1)));
    }

    [Fact]
    public async Task DefaultResiliencePipeline_CustomCircuitBreakerConcurrencyAndRateLimiterOptions_AreApplied()
    {
        bool opened = false;
        bool halfOpened = false;
        bool closed = false;
        bool rateRejected = false;

        System.Threading.RateLimiting.FixedWindowRateLimiter customLimiter = new(new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });

        ResiliencePipelineSettings customSettings = new()
        {
            BackoffType = DelayBackoffType.Constant,
            MaxRetryAttempts = 1,
            DelayInMilliseconds = 1,
            UseJitter = false,
            CircuitBreakerOptions = new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromMilliseconds(500),
                ShouldHandle = null!,
                OnOpened = _ =>
                {
                    opened = true;
                    return default;
                },
                OnHalfOpened = _ =>
                {
                    halfOpened = true;
                    return default;
                },
                OnClosed = _ =>
                {
                    closed = true;
                    return default;
                }
            },
            ConcurrencyLimiterOptions = new System.Threading.RateLimiting.ConcurrencyLimiterOptions
            {
                PermitLimit = 10,
                QueueLimit = 10
            },
            RateLimiterOptions = new Polly.RateLimiting.RateLimiterStrategyOptions
            {
                RateLimiter = args => customLimiter.AcquireAsync(1, args.Context.CancellationToken),
                OnRejected = _ =>
                {
                    rateRejected = true;
                    return default;
                }
            }
        };

        DefaultResiliencePipeline pipeline = new(
            NullLogger<DefaultResiliencePipeline>.Instance,
            customSettings,
            new PredicateBuilder().Handle<InvalidOperationException>());

        // First call succeeds (consumes the 1 rate-limit permit); subsequent rapid call hits rate limiter.
        int first = await pipeline.ExecuteAsync(static _ => ValueTask.FromResult(1));
        Assert.Equal(1, first);

        await Assert.ThrowsAsync<Polly.RateLimiting.RateLimiterRejectedException>(async () =>
            await pipeline.ExecuteAsync(static _ => ValueTask.FromResult(2)));
        Assert.True(rateRejected);

        // Separate pipeline without strict 1-permit limiter to exercise custom circuit breaker lifecycle.
        ResiliencePipelineSettings cbSettings = customSettings with { RateLimiterOptions = null };
        DefaultResiliencePipeline cbPipeline = new(
            NullLogger<DefaultResiliencePipeline>.Instance,
            cbSettings,
            new PredicateBuilder().Handle<InvalidOperationException>());

        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await cbPipeline.ExecuteAsync<int>(static _ => ValueTask.FromException<int>(new InvalidOperationException("cb-fail"))));
        }
        Assert.True(opened);

        await Task.Delay(550);

        int recovered = await cbPipeline.ExecuteAsync(static _ => ValueTask.FromResult(7));
        Assert.Equal(7, recovered);
        Assert.True(halfOpened);
        Assert.True(closed);

        customLimiter.Dispose();
    }

    [Theory]
    [InlineData(SmtpStatusCode.ServiceNotAvailable, 2)]
    [InlineData(SmtpStatusCode.MailboxBusy, 2)]
    [InlineData(SmtpStatusCode.InsufficientStorage, 2)]
    [InlineData(SmtpStatusCode.LocalErrorInProcessing, 2)]
    [InlineData(SmtpStatusCode.TransactionFailed, 2)]
    [InlineData(SmtpStatusCode.GeneralFailure, 2)]
    [InlineData(SmtpStatusCode.Ok, 1)]
    public async Task SmtpResiliencePipeline_HandlesTransientAndNonTransientStatusCodes(SmtpStatusCode statusCode, int expectedExecutions)
    {
        SmtpResiliencePipeline pipeline = new(
            NullLogger<SmtpResiliencePipeline>.Instance,
            FastSettings);

        int count = 0;
        if (expectedExecutions == 2)
        {
            bool ok = await pipeline.ExecuteAsync(async (ResilienceContext _) =>
            {
                await Task.Yield();
                count++;
                if (count == 1)
                    throw new SmtpException(statusCode, "smtp error");

                return true;
            });
            Assert.True(ok);
        }
        else
        {
            await Assert.ThrowsAsync<SmtpException>(async () =>
                await pipeline.ExecuteAsync(async (ResilienceContext _) =>
                {
                    await Task.Yield();
                    count++;
                    throw new SmtpException(statusCode, "non-transient smtp error");
                }));
        }

        Assert.Equal(expectedExecutions, count);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, null, 2)]
    [InlineData(HttpStatusCode.ServiceUnavailable, null, 2)]
    [InlineData(HttpStatusCode.TooManyRequests, null, 2)]
    [InlineData(HttpStatusCode.BadRequest, "SlowDown", 2)]
    [InlineData(HttpStatusCode.BadRequest, "RequestTimeout", 2)]
    [InlineData(HttpStatusCode.BadRequest, "RequestTimeTooSkewed", 2)]
    [InlineData(HttpStatusCode.Forbidden, "AccessDenied", 1)]
    public async Task AwsS3ResiliencePipeline_HandlesAdditionalTransientAndPermanentErrorCodes(HttpStatusCode statusCode, string? errorCode, int expectedExecutions)
    {
        AwsS3ResiliencePipeline pipeline = new(
            NullLogger<AwsS3ResiliencePipeline>.Instance,
            FastSettings);

        int count = 0;
        if (expectedExecutions == 2)
        {
            string res = await pipeline.ExecuteAsync(async (ResilienceContext _) =>
            {
                await Task.Yield();
                count++;
                if (count == 1)
                    throw new AmazonS3Exception("s3") { StatusCode = statusCode, ErrorCode = errorCode };

                return "ok";
            });
            Assert.Equal("ok", res);
        }
        else
        {
            await Assert.ThrowsAsync<AmazonS3Exception>(async () =>
                await pipeline.ExecuteAsync(async (ResilienceContext _) =>
                {
                    await Task.Yield();
                    count++;
                    throw new AmazonS3Exception("perm") { StatusCode = statusCode, ErrorCode = errorCode };
                }));
        }

        Assert.Equal(expectedExecutions, count);
    }
}
