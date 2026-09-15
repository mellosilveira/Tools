using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;

/// <summary>
/// Command that validates experimental data, selects the appropriate mechanical model orchestrator, 
/// and executes the curve fitting process.
/// </summary>
//public class FitCurve(
//    IExperimentalDataProcessor dataProcessor,
//    ICurveFitterFactory fitterFactory)
//    : CommandBaseWithData<FitCurveRequest, FitCurveResultData>
//{
//    /// <inheritdoc />
//    protected override async Task<Result<FitCurveResultData>> ExecuteCommandAsync(FitCurveRequest request)
//    {
//        await using var stream = request.ExperimentalData.OpenReadStream();

//        // Steps 1 & 2: Parse the CSV, validate 3 columns (time, strain, stress) of equal sizes, and segment it
//        var processingResult = await dataProcessor.ProcessAndSegmentAsync(stream).ConfigureAwait(false);

//        // Explicit validation check by the command
//        if (!processingResult.IsValid)
//        {
//            // Returns an HTTP 400 Bad Request to the user with the specific validation error
//            return Result.CreateBadRequest<FitCurveResultData>(processingResult.ErrorMessage!);
//        }

//        var segments = processingResult.Segments!;

//        // Step 3: Create the CurveFitter orchestrator based on model and physical considerations
//        var orchestrator = fitterFactory.Create(
//            request.MechanicalModelName,
//            request.RampTimeConsideration,
//            request.ViscoelasticEffect);

//        var curveFitInput = new CurveFitInput
//        {
//            InitialParameters = request.InitialParameters,
//            Segments = segments,
//            Options = request.Options
//        };

//        // Step 4: Execute the fit method which returns FitCurveResultData
//        var resultData = orchestrator.Fit(curveFitInput);

//        // Step 5: Build and return the successful result to the user
//        return Result.CreateSuccessOk(resultData);
//    }
//}

/// <summary>
/// Configuration options for the experimental data processing pipeline.
/// </summary>
public record DataProcessingOptions(
    double StartTimeThreshold,
    int? SkipPointsCount = null,
    double? SkipDeltaT = null
);

/// <summary>
/// Represents an immutable metadata segment of the experiment.
/// Instead of holding a List in memory, it provides a factory to stream its data asynchronously.
/// </summary>
public sealed record ExperimentalSegment(
    SegmentType Type,
    ExperimentalDataPoint StartPoint,
    ExperimentalDataPoint EndPoint,
    Func<CancellationToken, IAsyncEnumerable<ExperimentalDataPoint>> StreamPointsFactory)
{
    /// <summary>
    /// Streams the valid data points belonging to this segment, applying the configured downsampling (skips).
    /// </summary>
    public IAsyncEnumerable<ExperimentalDataPoint> GetValidPointsAsync(CancellationToken ct = default) => StreamPointsFactory(ct);
}

/// <summary>
/// Service responsible for processing experimental raw data streams and segmenting physical phases.
/// </summary>
public interface IExperimentalDataProcessorService
{
    /// <summary>
    /// Reads, normalizes, validates, and segments the strain and stress data streams.
    /// Writes valid data to a background file while yielding streaming segments.
    /// </summary>
    IAsyncEnumerable<ExperimentalSegment> ProcessExperimentalDataAsync(
        Stream strainStream,
        Stream stressStream,
        string validPointsOutputFilePath,
        DataProcessingOptions options,
        IProgress<double>? fileReadProgress,
        IProgress<long>? backgroundWriteProgress,
        CancellationToken cancellationToken = default);
}

public class ExperimentalDataProcessorService(ILogger<ExperimentalDataProcessorService> logger) : IExperimentalDataProcessorService
{
    private const double DerivativeZeroTolerance = 1e-6;
    private const double TimeTolerance = 1e-3;

    public async IAsyncEnumerable<ExperimentalSegment> ProcessExperimentalDataAsync(
        Stream strainStream,
        Stream stressStream,
        string validPointsOutputFilePath,
        DataProcessingOptions options,
        IProgress<double>? fileReadProgress,
        IProgress<long>? backgroundWriteProgress,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting experimental data processing with background writing to {FilePath}", validPointsOutputFilePath);

        long totalBytes = strainStream.Length + stressStream.Length;
        long processedBytes = 0;

        using var strainReader = new StreamReader(strainStream);
        using var stressReader = new StreamReader(stressStream);

        var writeChannel = Channel.CreateBounded<ExperimentalDataPoint>(new BoundedChannelOptions(50000)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        var backgroundWriterTask = StartBackgroundWriterAsync(writeChannel.Reader, validPointsOutputFilePath, backgroundWriteProgress, cancellationToken);

        ExperimentalDataPoint? segmentStartPoint = null;
        ExperimentalDataPoint? prevPoint = null;
        double prevStressDerivative = 0;
        SegmentType currentSegmentType = SegmentType.Unknown;
        double? firstValidTime = null;

        // In-memory buffer strictly for the downsampled points of the current segment
        List<ExperimentalDataPoint> currentSegmentPoints = new();
        int currentSkipCount = 0;
        double? lastYieldedTime = null;

        string? strainLine;
        string? stressLine;

        try
        {
            // Simultaneous reading of both files
            while ((strainLine = await strainReader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null
                && (stressLine = await stressReader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
            {
                processedBytes += strainLine.Length + stressLine.Length;
                if (processedBytes % 524288 == 0)
                    fileReadProgress?.Report((double)processedBytes / totalBytes * 100.0);

                var strainData = ParseLine(strainLine);
                var stressData = ParseLine(stressLine);

                // Skip point if timestamps do not match within the tolerance
                if (Math.Abs(strainData.Time - stressData.Time) > TimeTolerance)
                {
                    logger.LogTrace("Skipping point at StrainTime={StrainTime} and StressTime={StressTime} due to time mismatch.", strainData.Time, stressData.Time);
                    continue;
                }

                // Filtration Rules
                if (strainData.Time < options.StartTimeThreshold || strainData.Value <= 0)
                    continue;

                // Time Normalization
                if (!firstValidTime.HasValue)
                    firstValidTime = strainData.Time;

                double normalizedTime = strainData.Time - firstValidTime.Value;
                var point = new ExperimentalDataPoint(normalizedTime, stressData.Value, strainData.Value);

                if (!prevPoint.HasValue)
                {
                    prevPoint = point;
                    continue;
                }

                double dt = point.Time - prevPoint.Value.Time;
                if (dt <= 0) continue;

                double dStrain = (point.Strain - prevPoint.Value.Strain) / dt;
                double dStress = (point.Stress - prevPoint.Value.Stress) / dt;
                double d2Stress = (dStress - prevStressDerivative) / dt;

                // Logical Segmentation Analysis
                SegmentType calculatedType = DetermineSegmentType(dStrain, currentSegmentType);

                if (calculatedType != currentSegmentType)
                {
                    if (currentSegmentType != SegmentType.Unknown && segmentStartPoint.HasValue)
                    {
                        var segmentList = currentSegmentPoints;
                        yield return CreateSegment(currentSegmentType, segmentStartPoint.Value, prevPoint.Value, segmentList);
                    }

                    currentSegmentType = calculatedType;
                    segmentStartPoint = point;

                    // Reset buffers for the new segment
                    currentSegmentPoints = new List<ExperimentalDataPoint>();
                    currentSkipCount = 0;
                    lastYieldedTime = null;
                }

                // Physical Constraints Validation
                if (ValidateStress(calculatedType, dStress, d2Stress))
                {
                    // 1. Decoupled Write: All valid points go to the background file without skipping
                    await writeChannel.Writer.WriteAsync(point, cancellationToken).ConfigureAwait(false);

                    // 2. Memory Collection: Apply downsampling dynamically for the segment points
                    bool shouldKeep = true;

                    if (options.SkipPointsCount.HasValue && options.SkipPointsCount.Value > 0)
                    {
                        if (currentSkipCount < options.SkipPointsCount.Value)
                        {
                            currentSkipCount++;
                            shouldKeep = false;
                        }
                        else
                        {
                            currentSkipCount = 0;
                        }
                    }

                    if (shouldKeep && options.SkipDeltaT.HasValue && options.SkipDeltaT.Value > 0 && lastYieldedTime.HasValue)
                    {
                        if (point.Time - lastYieldedTime.Value < options.SkipDeltaT.Value)
                        {
                            shouldKeep = false;
                        }
                    }

                    if (shouldKeep)
                    {
                        currentSegmentPoints.Add(point);
                        lastYieldedTime = point.Time;
                    }
                }

                prevPoint = point;
                prevStressDerivative = dStress;
            }

            // Yield final segment
            if (currentSegmentType != SegmentType.Unknown && segmentStartPoint.HasValue && prevPoint.HasValue)
            {
                yield return CreateSegment(currentSegmentType, segmentStartPoint.Value, prevPoint.Value, currentSegmentPoints);
            }
        }
        finally
        {
            writeChannel.Writer.Complete();
            await backgroundWriterTask.ConfigureAwait(false);
            fileReadProgress?.Report(100.0);
        }
    }

    private ExperimentalSegment CreateSegment(
        SegmentType type,
        ExperimentalDataPoint start,
        ExperimentalDataPoint end,
        List<ExperimentalDataPoint> points)
    {
        return new ExperimentalSegment(
            Type: type,
            StartPoint: start,
            EndPoint: end,
            StreamPointsFactory: (ct) => StreamFromListAsync(points, ct)
        );
    }

    private async IAsyncEnumerable<ExperimentalDataPoint> StreamFromListAsync(
        List<ExperimentalDataPoint> points,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var pt in points)
        {
            ct.ThrowIfCancellationRequested();
            yield return pt;
        }

        await Task.CompletedTask.ConfigureAwait(false); // Ensures compliance with async interface expectations
    }

    private async Task StartBackgroundWriterAsync(
        ChannelReader<ExperimentalDataPoint> reader,
        string filePath,
        IProgress<long>? progress,
        CancellationToken ct)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 65536, FileOptions.Asynchronous);
            using var writer = new StreamWriter(fs) { AutoFlush = true };

            long written = 0;

            await foreach (var pt in reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                await writer.WriteLineAsync($"{pt.Time},{pt.Stress},{pt.Strain}").ConfigureAwait(false);
                written++;

                if (written % 1000 == 0)
                {
                    progress?.Report(written);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background writing to {FilePath} failed.", filePath);
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (double Time, double Value) ParseLine(string line)
    {
        var span = line.AsSpan();
        int commaIndex = span.IndexOf(',');
        return (double.Parse(span[..commaIndex]), double.Parse(span[(commaIndex + 1)..]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SegmentType DetermineSegmentType(double dStrain, SegmentType currentType)
    {
        if (dStrain > DerivativeZeroTolerance) return SegmentType.Ramp;
        if (Math.Abs(dStrain) <= DerivativeZeroTolerance && currentType == SegmentType.Ramp) return SegmentType.Relaxation;
        if (dStrain < -DerivativeZeroTolerance) return SegmentType.Descent;
        if (Math.Abs(dStrain) <= DerivativeZeroTolerance && currentType == SegmentType.Descent) return SegmentType.Recovery;

        return currentType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ValidateStress(SegmentType segment, double dStress, double d2Stress) => segment switch
    {
        SegmentType.Ramp => dStress > 0,
        SegmentType.Relaxation => dStress < 0 && d2Stress > 0,
        SegmentType.Descent => dStress < 0,
        SegmentType.Recovery => dStress > 0 && d2Stress < 0,
        _ => true
    };
}
