using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

public class ExperimentalDataService(
    ILogger<ExperimentalDataService> logger,
    IDifferentiation differentiation,
    IFileManager fileManager,
    ExperimentalDataSettings settings)
    : IExperimentalDataService
{
    public async Task<Result<(string OutputFileName, CurveSegment[] CurveSegments)>> ProcessAsync(
        string uniqueIdentifier,
        string outputFileUri,
        Stream strainStream,
        Stream stressStream,
        ExperimentalDataProcessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= ExperimentalDataProcessingOptions.Default;

        List<CurveSegment> curveSegments = [];
        List<double> timePoints = [];
        List<double> strainPoints = [];
        List<double> stressPoints = [];
        ProcessedDataPoint? previousPoint = null;
        SegmentType? previousSegmentType = null;

        (string outputFullFileName, Func<ProcessedDataPoint, CancellationToken, Task> writePointTask, Func<CancellationToken, Task> completeWriterTask) = await PrepareFileWriterAsync(outputFileUri, uniqueIdentifier, cancellationToken).ConfigureAwait(false);
        await foreach ((SegmentType segmentType, ProcessedDataPoint point) in SegmentPointsAsync(strainStream, stressStream, options, cancellationToken))
        {
            await writePointTask(point, cancellationToken).ConfigureAwait(false);

            bool typeChanged = previousSegmentType != segmentType;
            if (typeChanged && previousSegmentType != null && timePoints.Count > 0)
            {
                curveSegments.Add(new CurveSegment
                {
                    Type = previousSegmentType.Value,
                    TimePoints = [.. timePoints],
                    ExperimentalStrain = [.. strainPoints],
                    ExperimentalStress = [.. stressPoints]
                });

                timePoints.Clear();
                strainPoints.Clear();
                stressPoints.Clear();
            }

            if (typeChanged || previousPoint == null || (point.Time - previousPoint.Value.Time) >= options.SkipTimeStep)
            {
                timePoints.Add(point.Time);
                strainPoints.Add(point.Strain);
                stressPoints.Add(point.Stress);
            }

            previousSegmentType = segmentType;
            previousPoint = point;
        }

        await completeWriterTask(cancellationToken).ConfigureAwait(false);

        if (timePoints.Count > 0 && previousSegmentType != null)
        {
            curveSegments.Add(new CurveSegment
            {
                Type = previousSegmentType.Value,
                TimePoints = [.. timePoints],
                ExperimentalStrain = [.. strainPoints],
                ExperimentalStress = [.. stressPoints]
            });
        }

        return (outputFullFileName, [.. curveSegments]);
    }

    public async IAsyncEnumerable<SegmentedDataPoint> SegmentPointsAsync(
        Stream strainStream,
        Stream stressStream,
        ExperimentalDataProcessingOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= ExperimentalDataProcessingOptions.Default;

        await using CsvStreamReader strainReader = new(strainStream, leaveOpen: true);
        await using CsvStreamReader stressReader = new(stressStream, leaveOpen: true);

        double? firstValidTime = null;
        ProcessedDataPoint previousPoint = new();
        SegmentType currentSegmentType = SegmentType.Unknown;

        ExperimentalDataPoint[] buffer = new ExperimentalDataPoint[options.BufferSize];
        int bufferCount = 0;

        List<(SegmentType Type, ArraySegment<ExperimentalDataPoint> Points)> segmentResults = new(2);

        while (!cancellationToken.IsCancellationRequested)
        {
            double[]? strainRow = await strainReader.ReadNextRowAsync(cancellationToken).ConfigureAwait(false);
            double[]? stressRow = await stressReader.ReadNextRowAsync(cancellationToken).ConfigureAwait(false);

            if (strainRow is null || stressRow is null)
            {
                logger.LogTrace("Breaking loop due to end of stream or empty line.");
                break;
            }

            if (strainRow.Length < 2 || stressRow.Length < 2)
            {
                logger.LogWarning("CSV row must contain at least 2 columns (Time and Value). Skipping invalid row.");
                continue;
            }

            double time = strainRow[0];
            double strain = strainRow[1];
            if (time < options.StartTimeThreshold)
            {
                logger.LogTrace("Skipping point at Time={StrainTime} and Strain={Strain} due to start time threshold: {StartTimeThreshold}.", time, strain, options.StartTimeThreshold);
                continue;
            }

            double stressTime = stressRow[0];
            if (time.AbsolutRelativeDifference(stressTime) > options.RelativeTolerance)
            {
                logger.LogTrace("Skipping point at StrainTime={StrainTime} and StressTime={StressTime} due to time mismatch.", time, stressTime);
                continue;
            }

            firstValidTime ??= time;
            double normalizedTime = time - firstValidTime.Value;

            double stress = stressRow[1];
            if (strain <= options.Tolerance)
            {
                logger.LogTrace("Skipping point at StrainTime={StrainTime} and Strain={Strain} due to non-positive strain.", time, strain);
                previousPoint = new(normalizedTime, strain, StrainRate: 0, StrainAcceleration: 0, stress, StressRate: 0, StressAcceleration: 0);
                continue;
            }

            buffer[bufferCount++] = new ExperimentalDataPoint(Time: normalizedTime, Stress: stress, Strain: strain);
            if (bufferCount < options.BufferSize)
            {
                continue;
            }

            foreach ((SegmentType segmentType, ArraySegment<ExperimentalDataPoint> points) in ExtractSegments(currentSegmentType, buffer, bufferCount, options, segmentResults))
            {
                for (int i = 0; i < points.Count; i++)
                {
                    ProcessedDataPoint processedPoint = BuildProcessedDataPoint(previousPoint, points[i], options);
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
        }
    }

    public List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> ExtractSegments(
        SegmentType currentType,
        ExperimentalDataPoint[] points,
        int count,
        ExperimentalDataProcessingOptions? options = null)
        => ExtractSegments(currentType, points, count, options ?? ExperimentalDataProcessingOptions.Default, []);

    private async Task<(string OutputFullFileName, Func<ProcessedDataPoint, CancellationToken, Task> WritePointTask, Func<CancellationToken, Task> CompleteWriterTask)> PrepareFileWriterAsync(
        string outputFileUri,
        string uniqueIdentifier,
        CancellationToken cancellationToken)
    {
        FileInfo outputFile = fileManager.BuildTimebasedFileInfo(outputFileUri, uniqueIdentifier, FileExtensions.CommaSeparatedValues);
        using var streamWriter = fileManager.CreateLargeFileWriter(outputFile);
        await streamWriter.WriteLineAsync("Time,Strain,StrainRate,StrainAcceleration,Stress,StressRate,StressAcceleration").ConfigureAwait(false);

        ActionBlock<ProcessedDataPoint> fileWriterBlock = new(
            async p => await streamWriter.WriteLineAsync($"{p.Time},{p.Strain},{p.StrainRate},{p.StrainAcceleration},{p.Stress},{p.StressRate},{p.StressAcceleration}").ConfigureAwait(false),
            new ExecutionDataflowBlockOptions
            {
                // The file must be writer sequentially to avoid corruption, so we set MaxDegreeOfParallelism to 1.
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = settings.FileWriterBoundedCapacity,
                CancellationToken = cancellationToken
            });

        async Task WritePointAsync(ProcessedDataPoint point, CancellationToken ct)
        {
            if (!await fileWriterBlock.SendAsync(point, ct).ConfigureAwait(false))
                logger.LogWarning("Failed to send point {@Point} at Time={Time} to file writer block.", point, point.Time);
        }

        async Task CompleteWriterAsync(CancellationToken cancellationToken)
        {
            await streamWriter.DisposeAsync().ConfigureAwait(false);

            try
            {
                fileWriterBlock.Complete();
                await fileWriterBlock.Completion.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while completing the file writer block.");
            }
        }

        return (outputFile.FullName, WritePointAsync, CompleteWriterAsync);
    }

    private List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> ExtractSegments(
        SegmentType currentType,
        ExperimentalDataPoint[] buffer,
        int bufferCount,
        ExperimentalDataProcessingOptions options,
        List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> results)
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
            ? SliceBuffer(buffer, bufferCount, minStrainIndex, maxStrainIndex, SegmentType.Recovery, SegmentType.Ramp, SegmentType.Relaxation, results)
            : SliceBuffer(buffer, bufferCount, maxStrainIndex, minStrainIndex, SegmentType.Relaxation, SegmentType.Descent, SegmentType.Recovery, results);
    }

    private List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> SliceBuffer(
        ExperimentalDataPoint[] buffer,
        int bufferCount,
        int startIndex,
        int endIndex,
        SegmentType typeBefore,
        SegmentType activeType,
        SegmentType typeAfter,
        List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> results)
    {
        if (startIndex == 0 && endIndex == bufferCount - 1)
        {
            results.Add((activeType, new ArraySegment<ExperimentalDataPoint>(buffer, 0, bufferCount)));
            return results;
        }

        if (startIndex == 0)
        {
            results.Add((activeType, new ArraySegment<ExperimentalDataPoint>(buffer, 0, endIndex + 1)));
            results.Add((typeAfter, new ArraySegment<ExperimentalDataPoint>(buffer, endIndex + 1, bufferCount - (endIndex + 1))));
            return results;
        }

        if (endIndex == bufferCount - 1)
        {
            results.Add((typeBefore, new ArraySegment<ExperimentalDataPoint>(buffer, 0, startIndex)));
            results.Add((activeType, new ArraySegment<ExperimentalDataPoint>(buffer, startIndex, bufferCount - startIndex)));
            return results;
        }

        logger.LogError("Unexpected strain pattern: Start={StartIdx}, End={EndIdx}.", startIndex, endIndex);
        throw new InvalidOperationException($"Unexpected strain pattern: '{startIndex}' is not at the start and '{endIndex}' is not at the end of the buffer.");
    }

    private ProcessedDataPoint BuildProcessedDataPoint(ProcessedDataPoint basePoint, ExperimentalDataPoint point, ExperimentalDataProcessingOptions options)
    {
        double calculatedStrainRate = differentiation.Calculate(basePoint.Strain, point.Strain, point.Time - basePoint.Time);
        double calculatedStressRate = differentiation.Calculate(basePoint.Stress, point.Stress, point.Time - basePoint.Time);
        double calculatedStrainAcceleration = differentiation.Calculate(basePoint.StrainRate, calculatedStrainRate, point.Time - basePoint.Time);
        double calculatedStressAcceleration = differentiation.Calculate(basePoint.StressRate, calculatedStressRate, point.Time - basePoint.Time);

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
}