using MelloSilveiraTools.Core.Infrastructure.Logger;
using Polly;
using Polly.Retry;
using System.Net.Mail;

namespace MelloSilveiraTools.Core.Infrastructure.ResiliencePipelines;

/// <summary>
/// Resilience pipeline for SMTP communications that provides a strategy to handle retriable exceptions.
/// </summary>
public class SmtpResiliencePipeline : DefaultResiliencePipeline
{
    /// <summary>
    /// Initialize a new instance of <see cref="SmtpResiliencePipeline"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="settings"></param>
    public SmtpResiliencePipeline(ILogger logger, ResiliencePipelineSettings settings)
        : base(logger, settings, new PredicateBuilder()
            // TODO: AVALIAR QUANDO USAR SmtpException.
            //.Handle<SmtpException>()
            .Handle<SmtpFailedRecipientException>())
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="SmtpResiliencePipeline"/>.
    /// </summary>
    /// <param name="logger">See reference at <see cref="ILogger"/>.</param>
    /// <param name="settings">See reference at <see cref="ResiliencePipelineSettings"/>.</param>
    /// <param name="shouldHandle">Predicate that determines whether the retry should be executed for a given outcome.</param>
    public SmtpResiliencePipeline(ILogger logger, ResiliencePipelineSettings settings, Func<RetryPredicateArguments<object>, ValueTask<bool>> shouldHandle) : base(logger, settings, shouldHandle) { }
}