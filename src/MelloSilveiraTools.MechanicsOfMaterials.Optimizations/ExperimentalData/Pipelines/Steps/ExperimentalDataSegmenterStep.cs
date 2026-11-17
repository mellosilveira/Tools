using MelloSilveiraTools.Core.Files.Csv;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps;

/// <summary>
/// Pipeline step responsible for parsing and streaming raw experimental strain and stress data from persistent storage,
/// segmenting points into physical deformation phases (Ramp, Relaxation, Descent, Recovery) using numerical differentiation.
/// </summary>
/// <param name="logger">Logger for telemetry, warnings, and diagnostic information.</param>
/// <param name="differentiation">The differentiation calculator used to compute strain and stress rates and accelerations.</param>
/// <param name="fileStorageService">Storage service responsible for resolving and opening raw file streams.</param>
public sealed class ExperimentalDataSegmenterStep(
    ILogger<ExperimentalDataSegmenterStep> logger,
    IDifferentiation differentiation,
    IFileStorageService fileStorageService)
    : IAsyncEnumerablePipelineStep<ExperimentalDataSegmenterInput, SegmentedDataPoint>
{
    /// <inheritdoc/>
    public string Name => "ExperimentalDataSegmenter";

    /// <inheritdoc/>
    public async IAsyncEnumerable<SegmentedDataPoint> ExecuteAsync(ExperimentalDataSegmenterInput input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ExperimentalDataProcessingOptions options = input.Options;

        await using Stream strainStream = await fileStorageService.OpenReadAsync(input.StrainFileLocation, cancellationToken).ConfigureAwait(false);
        await using Stream stressStream = await fileStorageService.OpenReadAsync(input.StressFileLocation, cancellationToken).ConfigureAwait(false);

        // 'leaveOpen' is false so the readers own stream closure upon normal disposal; Stream 'await using' also guarantees cleanup if reader creation throws.
        await using CsvStreamReader strainReader = await CsvStreamReader.CreateAsync(strainStream, leaveOpen: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        await using CsvStreamReader stressReader = await CsvStreamReader.CreateAsync(stressStream, leaveOpen: false, cancellationToken: cancellationToken).ConfigureAwait(false);

        double? firstValidTime = null;
        ProcessedDataPoint? previousPoint = null;
        SegmentType currentSegmentType = SegmentType.Unknown;

        ExperimentalDataPoint[] buffer = ArrayPool<ExperimentalDataPoint>.Shared.Rent(options.BufferSize);
        int bufferCount = 0;

        List<(SegmentType Type, Range Range)> segmentResults = new(4);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadOnlyMemory<double> strainRow = await strainReader.ReadNextRowAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlyMemory<double> stressRow = await stressReader.ReadNextRowAsync(cancellationToken).ConfigureAwait(false);

                bool isEndOfStream = strainRow.Length < 2 || stressRow.Length < 2;
                if (!isEndOfStream)
                {
                    ReadOnlySpan<double> strainSpan = strainRow.Span;
                    ReadOnlySpan<double> stressSpan = stressRow.Span;

                    double time = strainSpan[0];
                    double strain = strainSpan[1];
                    if (time < input.StartExperimentalTimeThreshold)
                    {
                        logger.LogTrace("Skipping point at Time={StrainTime} and Strain={Strain} due to start time threshold: {StartTimeThreshold}.", time, strain, input.StartExperimentalTimeThreshold);
                        continue;
                    }

                    double stressTime = stressSpan[0];
                    if (time.AbsolutRelativeDifference(stressTime) > options.RelativeTolerance)
                    {
                        logger.LogTrace("Skipping point at StrainTime={StrainTime} and StressTime={StressTime} due to time mismatch.", time, stressTime);
                        continue;
                    }

                    if (bufferCount > 0 && time - buffer[bufferCount].Time <= options.Tolerance)
                    {
                        logger.LogTrace("Skipping point at Time={Time} due to timestamp too close to previous point: {LastTime}.", time, buffer[bufferCount].Time);
                        continue;
                    }

                    firstValidTime ??= time;
                    double normalizedTime = time - firstValidTime.Value;

                    double stress = stressSpan[1];
                    if (strain <= options.Tolerance)
                    {
                        logger.LogTrace("Skipping point at StrainTime={StrainTime} and Strain={Strain} due to non-positive strain.", time, strain);
                        previousPoint = new(normalizedTime, strain, StrainRate: 0, StrainAcceleration: 0, stress, StressRate: 0, StressAcceleration: 0);
                        continue;
                    }

                    buffer[bufferCount++] = new ExperimentalDataPoint(Time: normalizedTime, Stress: stress, Strain: strain);
                    if (bufferCount < options.BufferSize)
                        continue;
                }
                else
                {
                    logger.LogTrace("Breaking loop due to end of stream or empty line.");
                    if (bufferCount == 0)
                        break;
                }

                segmentResults = ExtractSegments(segmentResults, currentSegmentType, buffer.AsSpan(0, bufferCount), in options);
                foreach ((SegmentType segmentType, Range range) in segmentResults)
                {
                    int start = range.Start.Value;
                    int end = range.End.Value;
                    for (int i = start; i < end; i++)
                    {
                        ref ExperimentalDataPoint experimentalDataPoint = ref buffer[i];
                        ProcessedDataPoint processedPoint = previousPoint is null ? CreateBaselinePoint(in experimentalDataPoint, in options) : BuildProcessedDataPoint(previousPoint.Value, in experimentalDataPoint, in options);

                        if (!ValidateStress(segmentType, processedPoint.StressRate, processedPoint.StressAcceleration))
                        {
                            logger.LogWarning("Invalid stress behavior detected for point: {@Point}.", processedPoint);
                            continue;
                        }

                        yield return new SegmentedDataPoint(segmentType, processedPoint);
                        previousPoint = processedPoint;
                        currentSegmentType = segmentType;
                    }
                }

                bufferCount = 0;

                if (isEndOfStream)
                    break;
            }

            yield return new SegmentedDataPoint(SegmentType.Unknown, default);
        }
        finally
        {
            ArrayPool<ExperimentalDataPoint>.Shared.Return(buffer);
        }
    }

    private List<(SegmentType Type, Range Range)> ExtractSegments(List<(SegmentType Type, Range Range)> results, SegmentType currentType, ReadOnlySpan<ExperimentalDataPoint> buffer, in ExperimentalDataProcessingOptions options)
    {
        results.Clear();
        int bufferCount = buffer.Length;
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
        double strainRate = differentiation.Calculate(minStrain, maxStrain, stepTime > options.Tolerance ? stepTime : double.Epsilon);

        if (Math.Abs(strainRate) <= options.RateTolerance)
        {
            SegmentType type = currentType is SegmentType.Descent or SegmentType.Recovery ? SegmentType.Recovery : SegmentType.Relaxation;
            results.Add((type, 0..bufferCount));
            return results;
        }

        return strainRate > options.RateTolerance
            ? SliceBuffer(results, bufferCount, minStrainIndex, maxStrainIndex, SegmentType.Recovery, SegmentType.Ramp, SegmentType.Relaxation)
            : SliceBuffer(results, bufferCount, maxStrainIndex, minStrainIndex, SegmentType.Relaxation, SegmentType.Descent, SegmentType.Recovery);
    }

    private List<(SegmentType Type, Range Range)> SliceBuffer(
        List<(SegmentType Type, Range Range)> results,
        int bufferCount,
        int startIndex,
        int endIndex,
        SegmentType typeBefore,
        SegmentType activeType,
        SegmentType typeAfter)
    {
        if (startIndex < 0 || endIndex >= bufferCount || startIndex > endIndex)
        {
            logger?.LogError("Unexpected strain pattern: Start={StartIdx}, End={EndIdx}.", startIndex, endIndex);
            throw new InvalidOperationException($"Unexpected strain pattern: '{startIndex}' is not at the start and '{endIndex}' is not at the end of the buffer.");
        }

        if (startIndex > 0)
            results.Add((typeBefore, 0..startIndex));

        results.Add((activeType, startIndex..(endIndex + 1)));

        if (endIndex < bufferCount - 1)
            results.Add((typeAfter, (endIndex + 1)..bufferCount));

        return results;
    }

    private ProcessedDataPoint BuildProcessedDataPoint(ProcessedDataPoint basePoint, in ExperimentalDataPoint point, in ExperimentalDataProcessingOptions options)
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

    private static ProcessedDataPoint CreateBaselinePoint(in ExperimentalDataPoint point, in ExperimentalDataProcessingOptions options) => new(
        point.Time,
        Strain: Math.Abs(point.Strain) > options.Tolerance ? point.Strain : 0,
        StrainRate: 0,
        StrainAcceleration: 0,
        Stress: Math.Abs(point.Stress) > options.Tolerance ? point.Stress : 0,
        StressRate: 0,
        StressAcceleration: 0);

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
