using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Factory entry point.
/// </summary>
public static class DataflowPipelineFactory
{
    /// <summary>
    /// Initializes a new Dataflow Pipeline utilizing a BufferBlock as the ingestion head.
    /// </summary>
    public static DataflowBuilder<T, T> Start<T>(ILogger? logger = null, int initialBufferSize = 10000)
    {
        BufferBlock<T> buffer = new(new DataflowBlockOptions { BoundedCapacity = initialBufferSize });
        return new DataflowBuilder<T, T>(buffer, buffer, logger);
    }
}