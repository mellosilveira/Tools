using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;

/// <summary>
/// Handles the validation and segmentation of the experimental data file.
/// </summary>
public interface IExperimentalDataProcessor
{
    Task<Result<CurveSegment[]>> ProcessAsync(Stream strainStream, Stream stressStream, ExperimentalDataProcessingOptions options, CancellationToken cancellationToken);
}

public readonly record struct ProcessedDataPoint(
    double Time,
    double Strain,
    double StrainRate,
    double StrainAcceleration,
    double Stress,
    double StressRate,
    double StressAcceleration)
{
    public static implicit operator ExperimentalDataPoint(ProcessedDataPoint point) => new(point.Time, point.Strain, point.Stress);
}

public readonly record struct SegmentedDataPoint(SegmentType SegmentType, ProcessedDataPoint ProcessedDataPoint)
{
    public static implicit operator ExperimentalDataPoint(SegmentedDataPoint point) => point.ProcessedDataPoint;
    public static implicit operator ProcessedDataPoint(SegmentedDataPoint point) => point.ProcessedDataPoint;
}

public record ExperimentalDataProcessingOptions(
    double StartTimeThreshold,
    ushort BufferSize = 10,
    double Tolerance = MathematicConstants.Tolerance,
    double RelativeTolerance = MathematicConstants.RelativeTolerance,
    double DerivativeTolerance = MathematicConstants.Tolerance,
    double SkipTimeStep = 0);

public class ExperimentalDataProcessor(
    ILogger<ExperimentalDataProcessor> logger,
    IDifferentiation differentiation)
    : IExperimentalDataProcessor
{
    public async Task<Result<CurveSegment[]>> ProcessAsync(Stream strainStream, Stream stressStream, ExperimentalDataProcessingOptions options, CancellationToken cancellationToken)
    {
        return null;
    }

    public async IAsyncEnumerable<ProcessedDataPoint> ProcessPointsAsync(Stream strainStream, Stream stressStream, ExperimentalDataProcessingOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using (var strainReader = new StreamReader(strainStream))
        using (var stressReader = new StreamReader(stressStream))
        {
            string? strainLine;
            string? stressLine;
            double? firstValidTime = null;
            ProcessedDataPoint previousPoint = new();
            SegmentType currentSegmentType = SegmentType.Unknown;
            List<ExperimentalDataPoint> bufferPoints = [];

            // TODO: TENTAR PEGAR MAIS PONTOS DE UMA ÚNICA VEZ PARA SER MAIS ASSERTIVO.

            while ((strainLine = await strainReader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null
                && (stressLine = await stressReader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
            {
                if (string.IsNullOrWhiteSpace(strainLine))
                {
                    logger.LogTrace("Breaking loop due to empty strain line.");
                    break;
                }

                var (time, strain) = ParseLine(strainLine);
                if (time < options.StartTimeThreshold)
                {
                    logger.LogTrace("Skipping point at StrainTime={StrainTime} and Strain={Strain} due to start time threshold.", time, strain);
                    continue;
                }

                var (stressTime, stress) = ParseLine(stressLine);

                // Skip point if timestamps do not match within the tolerance
                if (time.RelativeAbsolutDifference(stressTime) > options.RelativeTolerance)
                {
                    logger.LogTrace("Skipping point at StrainTime={StrainTime} and StressTime={StressTime} due to time mismatch.", time, stressTime);
                    continue;
                }

                // Time Normalization
                firstValidTime ??= time;
                double normalizedTime = time - firstValidTime.Value;

                if (strain <= options.Tolerance)
                {
                    logger.LogTrace("Skipping point at StrainTime={StrainTime} and Strain={Strain} due to non-positive strain.", time, strain);
                    previousPoint = new(normalizedTime, strain, StrainRate: 0, StrainAcceleration: 0, stress, StressRate: 0, StressAcceleration: 0);
                    continue;
                }

                if (bufferPoints.Count < options.BufferSize)
                {
                    bufferPoints.Add(new ExperimentalDataPoint(normalizedTime, strain, stress));
                }
                else
                {


                    bufferPoints = [];
                }


            }
        }

        yield break;
    }

    private static (double Time, double Value) ParseLine(string line)
    {
        var span = line.AsSpan();
        int commaIndex = span.IndexOf(',');
        return (double.Parse(span[..commaIndex]), double.Parse(span[(commaIndex + 1)..]));
    }

    private static SegmentType DetermineSegmentType(double dStrain, SegmentType currentType, double derivativeTolerance)
    {
        if (dStrain > derivativeTolerance) return SegmentType.Ramp;
        if (Math.Abs(dStrain) <= derivativeTolerance && currentType == SegmentType.Ramp) return SegmentType.Relaxation;
        if (dStrain < -derivativeTolerance) return SegmentType.Descent;
        if (Math.Abs(dStrain) <= derivativeTolerance && currentType == SegmentType.Descent) return SegmentType.Recovery;

        return currentType;
    }

    private static bool ValidateStress(SegmentType segment, double stressRate, double stressAcceleration) => segment switch
    {
        SegmentType.Ramp => stressRate > 0,
        SegmentType.Relaxation => stressRate < 0 && stressAcceleration > 0,
        SegmentType.Descent => stressRate < 0,
        SegmentType.Recovery => stressRate > 0 && stressAcceleration < 0,
        _ => false
    };

    public Dictionary<SegmentType, ExperimentalDataPoint[]> DetermineSegmentType(SegmentType currentType, ExperimentalDataPoint[] buffer, ExperimentalDataProcessingOptions options)
    {
        int minStrainIndex = 0, maxStrainIndex = 0;
        double minStrain = buffer[0].Strain, maxStrain = buffer[0].Strain;

        for (int i = 1; i < buffer.Length; i++)
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

        if (Math.Abs(strainRate) <= options.DerivativeTolerance)
        {
            var type = currentType is SegmentType.Descent or SegmentType.Recovery ? SegmentType.Recovery : SegmentType.Relaxation;
            return new() { { type, buffer } };
        }

        return strainRate > options.DerivativeTolerance
            ? SliceBuffer(buffer, minStrainIndex, maxStrainIndex, SegmentType.Recovery, SegmentType.Ramp, SegmentType.Relaxation)
            : SliceBuffer(buffer, maxStrainIndex, minStrainIndex, SegmentType.Relaxation, SegmentType.Descent, SegmentType.Recovery);
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
            StrainRate: Math.Abs(calculatedStrainRate) > options.DerivativeTolerance ? calculatedStrainRate : 0,
            StrainAcceleration: Math.Abs(calculatedStrainAcceleration) > options.DerivativeTolerance ? calculatedStrainAcceleration : 0,
            Stress: Math.Abs(point.Stress) > options.Tolerance ? point.Stress : 0,
            StressRate: Math.Abs(calculatedStressRate) > options.DerivativeTolerance ? calculatedStressRate : 0,
            StressAcceleration: Math.Abs(calculatedStressAcceleration) > options.DerivativeTolerance ? calculatedStressAcceleration : 0
        );
    }

    private Dictionary<SegmentType, ExperimentalDataPoint[]> SliceBuffer(
        ExperimentalDataPoint[] points,
        int startIndex,
        int endIndex,
        SegmentType segmentTypeBefore,
        SegmentType activeSegmentyType,
        SegmentType segmentTypeAfter)
    {
        if (startIndex == 0 && endIndex == points.Length - 1)
            return new() { { activeSegmentyType, points } };

        if (startIndex == 0)
            return new() { { activeSegmentyType, points[..(endIndex + 1)] }, { segmentTypeAfter, points[(endIndex + 1)..] } };

        if (endIndex == points.Length - 1)
            return new() { { segmentTypeBefore, points[..startIndex] }, { activeSegmentyType, points[startIndex..] } };

        logger.LogError("Unexpected strain pattern: Start={StartIdx}, End={EndIdx}.", startIndex, endIndex);
        throw new InvalidOperationException($"Unexpected strain pattern: '{startIndex}' is not at the start and '{endIndex}' is not at the end of the buffer.");
    }
}