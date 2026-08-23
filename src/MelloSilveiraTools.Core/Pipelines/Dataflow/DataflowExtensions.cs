using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

public static class DataflowExtensions
{
    extension<TTail>(ISourceBlock<TTail> source)
    {
        public IDisposable LinkTo(ITargetBlock<TTail> target, bool ignoreNulls, bool propagateCompletion = true) => ignoreNulls
            ? source.LinkTo(target, item => item is not null, propagateCompletion)
            : source.LinkTo(target, propagateCompletion);

        public IDisposable LinkTo(ITargetBlock<TTail> target, Predicate<TTail> predicate, bool propagateCompletion = true)
            => source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = propagateCompletion }, predicate);

        public IDisposable LinkTo(ITargetBlock<TTail> target, bool propagateCompletion = true)
            => source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = propagateCompletion });
    }
}
