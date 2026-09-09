using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Provides streamlined linkage operations for TPL Dataflow blocks.
/// </summary>
/// <remarks>
/// Technical Decision: These extensions intentionally abstract the instantiation of <see cref="DataflowLinkOptions"/> 
/// to enforce <c>PropagateCompletion = true</c> by default. This ensures that a completion or fault signal at the 
/// head of the pipeline natively propagates and drains through the entire execution graph, preventing orphaned blocks.
/// </remarks>
public static class DataflowExtensions
{
    extension<TTail>(ISourceBlock<TTail> source)
    {
        /// <summary>
        /// Links the source to a target block, routing only messages that satisfy the provided predicate.
        /// </summary>
        /// <param name="target">The downstream block receiving the messages.</param>
        /// <param name="predicate">The condition a message must meet to be forwarded.</param>
        /// <param name="propagateCompletion">Whether to pass completion and fault signals downstream. Defaults to true.</param>
        /// <remarks>
        /// Technical Decision: Reduces pipeline boilerplate by encapsulating the <see cref="DataflowLinkOptions"/> allocation 
        /// alongside the predicate evaluation.
        /// Limitation: In TPL Dataflow, if an item fails the predicate and there is no secondary fallback link attached to the 
        /// block (e.g., a <see cref="DataflowBlock.NullTarget{T}"/> or a fallback node), the rejected message remains permanently 
        /// stuck in the source block's output buffer. This will eventually exhaust the bounded capacity and cause a pipeline deadlock. 
        /// Use with strict caution in branching topologies.
        /// </remarks>
        public IDisposable LinkTo(ITargetBlock<TTail> target, Predicate<TTail> predicate, bool propagateCompletion = true)
            => source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = propagateCompletion }, predicate);

        /// <summary>
        /// Links the source to a target block, overriding native TPL behavior to propagate completion by default.
        /// </summary>
        /// <param name="target">The downstream block receiving the messages.</param>
        /// <param name="propagateCompletion">Whether to pass completion and fault signals downstream. Defaults to true.</param>
        /// <remarks>
        /// Technical Decision: The native <see cref="DataflowBlock.LinkTo{TOutput}(ISourceBlock{TOutput}, ITargetBlock{TOutput}, DataflowLinkOptions, Predicate{TOutput})"/> implementation defaults to 
        /// <c>PropagateCompletion = false</c>. This overload explicitly flips that default to guarantee graceful pipeline 
        /// teardowns without requiring developers to repeatedly manually allocate <see cref="DataflowLinkOptions"/> at every step.
        /// </remarks>
        public IDisposable LinkTo(ITargetBlock<TTail> target, bool propagateCompletion = true)
            => source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = propagateCompletion });
    }
}