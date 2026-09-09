using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

/// <summary>
/// Pipeline step responsible for parsing and streaming raw experimental strain and stress data,
/// segmenting points into physical deformation phases (Ramp, Relaxation, Descent, Recovery) using numerical differentiation.
/// </summary>
/// <param name="logger">Logger for telemetry, warnings, and diagnostic information.</param>
/// <param name="differentiation">The differentiation calculator used to compute strain and stress rates and accelerations.</param>
/// <param name="options">Options controlling tolerances, start time thresholds, and buffer sizing.</param>
public sealed class ExperimentalDataSegmenterStep(
    ILogger logger,
    IDifferentiation differentiation,
    ExperimentalDataProcessingOptions options)
    : IAsyncEnumerablePipelineStep<(Stream StrainStream, Stream StressStream), SegmentedDataPoint>
{
    /// <inheritdoc/>
    public string Name => "ExperimentalDataSegmenter";

    /// <inheritdoc/>
    public async IAsyncEnumerable<SegmentedDataPoint> ExecuteAsync((Stream StrainStream, Stream StressStream) input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        (Stream strainStream, Stream stressStream) = input;
        await using CsvStreamReader strainReader = new(strainStream, leaveOpen: true);
        await using CsvStreamReader stressReader = new(stressStream, leaveOpen: true);

        double? firstValidTime = null;
        ProcessedDataPoint previousPoint = new();
        SegmentType currentSegmentType = SegmentType.Unknown;

        ExperimentalDataPoint[] buffer = ArrayPool<ExperimentalDataPoint>.Shared.Rent(options.BufferSize);
        int bufferCount = 0;

        List<(SegmentType Type, ArraySegment<ExperimentalDataPoint> Points)> segmentResults = new(4);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                double[]? strainRow = await strainReader.ReadNextRowAsync(cancellationToken).ConfigureAwait(false);
                double[]? stressRow = await stressReader.ReadNextRowAsync(cancellationToken).ConfigureAwait(false);

                bool isEndOfStream = strainRow is null || stressRow is null;
                if (!isEndOfStream)
                {
                    if (strainRow!.Length < 2 || stressRow!.Length < 2)
                    {
                        logger?.LogWarning("CSV row must contain at least 2 columns (Time and Value). Skipping invalid row.");
                        continue;
                    }

                    double time = strainRow[0];
                    double strain = strainRow[1];
                    if (time < options.StartTimeThreshold)
                    {
                        logger?.LogTrace("Skipping point at Time={StrainTime} and Strain={Strain} due to start time threshold: {StartTimeThreshold}.", time, strain, options.StartTimeThreshold);
                        continue;
                    }

                    double stressTime = stressRow[0];
                    if (time.AbsolutRelativeDifference(stressTime) > options.RelativeTolerance)
                    {
                        logger?.LogTrace("Skipping point at StrainTime={StrainTime} and StressTime={StressTime} due to time mismatch.", time, stressTime);
                        continue;
                    }

                    firstValidTime ??= time;
                    double normalizedTime = time - firstValidTime.Value;

                    double stress = stressRow[1];
                    if (strain <= options.Tolerance)
                    {
                        logger?.LogTrace("Skipping point at StrainTime={StrainTime} and Strain={Strain} due to non-positive strain.", time, strain);
                        previousPoint = new(normalizedTime, strain, StrainRate: 0, StrainAcceleration: 0, stress, StressRate: 0, StressAcceleration: 0);
                        continue;
                    }

                    buffer[bufferCount++] = new ExperimentalDataPoint(Time: normalizedTime, Stress: stress, Strain: strain);
                    if (bufferCount < options.BufferSize)
                    {
                        continue;
                    }
                }
                else
                {
                    logger?.LogTrace("Breaking loop due to end of stream or empty line.");
                    if (bufferCount == 0)
                    {
                        break;
                    }
                }

                foreach ((SegmentType segmentType, ArraySegment<ExperimentalDataPoint> points) in ExtractSegments(differentiation, currentSegmentType, buffer, bufferCount, options, segmentResults, logger))
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        ProcessedDataPoint processedPoint = BuildProcessedDataPoint(differentiation, previousPoint, points[i], options);
                        if (!ValidateStress(segmentType, processedPoint.StressRate, processedPoint.StressAcceleration))
                        {
                            logger?.LogWarning("Invalid stress behavior detected for point: {@Point}.", processedPoint);
                            continue;
                        }

                        yield return new SegmentedDataPoint(segmentType, processedPoint);
                        previousPoint = processedPoint;
                        currentSegmentType = segmentType;
                    }
                }

                bufferCount = 0;

                if (isEndOfStream)
                {
                    break;
                }
            }
        }
        finally
        {
            ArrayPool<ExperimentalDataPoint>.Shared.Return(buffer);
        }
    }

    private static List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> ExtractSegments(
        IDifferentiation differentiation,
        SegmentType currentType,
        ExperimentalDataPoint[] buffer,
        int bufferCount,
        ExperimentalDataProcessingOptions options,
        List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> results,
        ILogger? logger)
    {
        results.Clear();
        int minStrainIndex = 0, maxStrainIndex = 0;
        double minStrain = buffer[0].Strain, maxStrain = buffer[0].Strain;

        for (int i = 1; i < bufferCount; i++)
        {
            double strainDiff = buffer[i].Strain - buffer[i - 1].Strain;
            if (Math.Abs(strainDiff) < options.Tolerance)
            {
                if (currentType == SegmentType.Relaxation)
                {
                    maxStrain = buffer[i].Strain;
                    maxStrainIndex = i;
                }
                else if (currentType == SegmentType.Recovery)
                {
                    minStrain = buffer[i].Strain;
                    minStrainIndex = i;
                }
            }
            else
            {
                if (buffer[i].Strain > maxStrain)
                {
                    maxStrain = buffer[i].Strain;
                    maxStrainIndex = i;
                }

                if (buffer[i].Strain < minStrain)
                {
                    minStrain = buffer[i].Strain;
                    minStrainIndex = i;
                }
            }
        }

        double stepTime = buffer[maxStrainIndex].Time - buffer[minStrainIndex].Time;
        double strainRate = differentiation.Calculate(minStrain, maxStrain, stepTime == 0 ? double.Epsilon : stepTime);

        if (Math.Abs(strainRate) <= options.RateTolerance)
        {
            SegmentType type = currentType is SegmentType.Descent or SegmentType.Recovery ? SegmentType.Recovery : SegmentType.Relaxation;
            results.Add((type, new ArraySegment<ExperimentalDataPoint>(buffer, 0, bufferCount)));
            return results;
        }

        return strainRate > options.RateTolerance
            ? SliceBuffer(buffer, bufferCount, minStrainIndex, maxStrainIndex, SegmentType.Recovery, SegmentType.Ramp, SegmentType.Relaxation, results, logger)
            : SliceBuffer(buffer, bufferCount, maxStrainIndex, minStrainIndex, SegmentType.Relaxation, SegmentType.Descent, SegmentType.Recovery, results, logger);
    }

    private static List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> SliceBuffer(
        ExperimentalDataPoint[] buffer,
        int bufferCount,
        int startIndex,
        int endIndex,
        SegmentType typeBefore,
        SegmentType activeType,
        SegmentType typeAfter,
        List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> results,
        ILogger? logger)
    {
        if (startIndex < 0 || endIndex >= bufferCount || startIndex > endIndex)
        {
            logger?.LogError("Unexpected strain pattern: Start={StartIdx}, End={EndIdx}.", startIndex, endIndex);
            throw new InvalidOperationException($"Unexpected strain pattern: '{startIndex}' is not at the start and '{endIndex}' is not at the end of the buffer.");
        }

        return results
            .FluentAddIf(startIndex > 0, (typeBefore, new ArraySegment<ExperimentalDataPoint>(buffer, 0, startIndex)))
            .FluentAdd((activeType, new ArraySegment<ExperimentalDataPoint>(buffer, startIndex, (endIndex + 1) - startIndex)))
            .FluentAddIf(endIndex < bufferCount - 1, (typeAfter, new ArraySegment<ExperimentalDataPoint>(buffer, endIndex + 1, bufferCount - (endIndex + 1))));
    }

    private static ProcessedDataPoint BuildProcessedDataPoint(IDifferentiation differentiation, ProcessedDataPoint basePoint, ExperimentalDataPoint point, ExperimentalDataProcessingOptions options)
    {
        double timeDelta = point.Time - basePoint.Time;
        double calculatedStrainRate = differentiation.Calculate(basePoint.Strain, point.Strain, timeDelta);
        double calculatedStressRate = differentiation.Calculate(basePoint.Stress, point.Stress, timeDelta);
        double calculatedStrainAcceleration = differentiation.Calculate(basePoint.StrainRate, calculatedStrainRate, timeDelta);
        double calculatedStressAcceleration = differentiation.Calculate(basePoint.StressRate, calculatedStressRate, timeDelta);

        return new ProcessedDataPoint(
            point.Time,
            Strain: Math.Abs(point.Strain) > options.Tolerance ? point.Strain : 0,
            StrainRate: Math.Abs(calculatedStrainRate) > options.RateTolerance ? calculatedStrainRate : 0,
            StrainAcceleration: Math.Abs(calculatedStrainAcceleration) > options.AccelerationTolerance ? calculatedStrainAcceleration : 0,
            Stress: Math.Abs(point.Stress) > options.Tolerance ? point.Stress : 0,
            StressRate: Math.Abs(calculatedStressRate) > options.RateTolerance ? calculatedStressRate : 0,
            StressAcceleration: Math.Abs(calculatedStressAcceleration) > options.AccelerationTolerance ? calculatedStressAcceleration : 0
        );
    }

    private static bool ValidateStress(SegmentType segment, double stressRate, double stressAcceleration) => segment switch
    {
        SegmentType.Ramp => stressRate > 0,
        SegmentType.Relaxation => stressRate <= 0 && stressAcceleration >= 0,
        SegmentType.Descent => stressRate < 0,
        SegmentType.Recovery => stressRate >= 0 && stressAcceleration <= 0,
        _ => false
    };

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
