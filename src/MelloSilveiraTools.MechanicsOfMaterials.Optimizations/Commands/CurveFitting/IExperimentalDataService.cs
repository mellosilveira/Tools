using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;

/// <summary>
/// Handles the validation and segmentation of the experimental data file.
/// </summary>
public interface IExperimentalDataService
{
    Task<Result<CurveSegment[]>> ProcessAsync(string identifier, string outputFileUri, Stream strainStream, Stream stressStream, ExperimentalDataProcessingOptions options, CancellationToken cancellationToken);
}

public class ExperimentalDataService(
    ILogger<ExperimentalDataService> logger,
    IDifferentiation differentiation,
    IFileManager fileManager)
    : IExperimentalDataService
{
    public async Task<Result<CurveSegment[]>> ProcessAsync(string identifier, string outputFileUri, Stream strainStream, Stream stressStream, ExperimentalDataProcessingOptions options, CancellationToken cancellationToken)
    {
        using (var streamWriter = fileManager.CreateTimebasedFileWriter(outputFileUri, identifier, FileExtensions.CommaSeparatedValues))
        {
            await foreach ((SegmentType segmentType, ProcessedDataPoint processedDataPoint) in SegmentPointsAsync(strainStream, stressStream, options, cancellationToken))
            {

            }
        }


        return null;
    }

    public async IAsyncEnumerable<SegmentedDataPoint> SegmentPointsAsync(
        Stream strainStream,
        Stream stressStream,
        ExperimentalDataProcessingOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var strainReader = new StreamReader(strainStream);
        using var stressReader = new StreamReader(stressStream);

        string? strainLine;
        string? stressLine;
        double? firstValidTime = null;
        ProcessedDataPoint previousPoint = new();
        SegmentType currentSegmentType = SegmentType.Unknown;

        // 1. Substituímos o List por um Array Fixo para reaproveitamento total
        ExperimentalDataPoint[] buffer = new ExperimentalDataPoint[options.BufferSize];
        int bufferCount = 0;

        // 2. Pré-alocamos a lista de resultados para não recriar dicionários e arrays
        List<(SegmentType Type, ArraySegment<ExperimentalDataPoint> Points)> segmentResults = new(2);

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
                logger.LogTrace("Skipping point at Time={StrainTime} and Strain={Strain} due to start time threshold: {StartTimeThreshold}.", time, strain, options.StartTimeThreshold);
                continue;
            }

            var (stressTime, stress) = ParseLine(stressLine);
            if (time.AbsolutRelativeDifference(stressTime) > options.RelativeTolerance)
            {
                logger.LogTrace("Skipping point at StrainTime={StrainTime} and StressTime={StressTime} due to time mismatch.", time, stressTime);
                continue;
            }

            firstValidTime ??= time;
            double normalizedTime = time - firstValidTime.Value;

            if (strain <= options.Tolerance)
            {
                logger.LogTrace("Skipping point at StrainTime={StrainTime} and Strain={Strain} due to non-positive strain.", time, strain);
                previousPoint = new(normalizedTime, strain, StrainRate: 0, StrainAcceleration: 0, stress, StressRate: 0, StressAcceleration: 0);
                continue;
            }

            // Correção de Bug: Adiciona ao buffer ANTES de verificar se está cheio para não perder o ponto atual
            buffer[bufferCount++] = new ExperimentalDataPoint(normalizedTime, strain, stress);
            if (bufferCount < options.BufferSize)
                continue;

            // Processa passando o array fixo e a lista pre-alocada
            foreach (var (segmentType, points) in ExtractSegments(currentSegmentType, buffer, bufferCount, options, segmentResults))
            {
                // ArraySegment permite indexação super rápida sem alocar arrays
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

            // Apenas zeramos o contador. Os dados serão sobrescritos de forma eficiente na próxima volta.
            bufferCount = 0;
        }
    }

    private static (double Time, double Value) ParseLine(string line)
    {
        var span = line.AsSpan();
        int commaIndex = span.IndexOf(',');
        return (double.Parse(span[..commaIndex]), double.Parse(span[(commaIndex + 1)..]));
    }

    public List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> ExtractSegments(
        SegmentType currentType,
        ExperimentalDataPoint[] buffer,
        int count,
        ExperimentalDataProcessingOptions options,
        List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> results)
    {
        results.Clear();
        int minStrainIndex = 0, maxStrainIndex = 0;
        double minStrain = buffer[0].Strain, maxStrain = buffer[0].Strain;

        // Iteramos até 'count' em vez de 'buffer.Length'
        for (int i = 1; i < count; i++)
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
            results.Add((type, new ArraySegment<ExperimentalDataPoint>(buffer, 0, count)));
            return results;
        }

        return strainRate > options.DerivativeTolerance
            ? SliceBuffer(buffer, count, minStrainIndex, maxStrainIndex, SegmentType.Recovery, SegmentType.Ramp, SegmentType.Relaxation, results)
            : SliceBuffer(buffer, count, maxStrainIndex, minStrainIndex, SegmentType.Relaxation, SegmentType.Descent, SegmentType.Recovery, results);
    }

    private List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> SliceBuffer(
        ExperimentalDataPoint[] buffer,
        int count,
        int startIndex,
        int endIndex,
        SegmentType typeBefore,
        SegmentType activeType,
        SegmentType typeAfter,
        List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> results)
    {
        if (startIndex == 0 && endIndex == count - 1)
        {
            results.Add((activeType, new ArraySegment<ExperimentalDataPoint>(buffer, 0, count)));
            return results;
        }

        if (startIndex == 0)
        {
            results.Add((activeType, new ArraySegment<ExperimentalDataPoint>(buffer, 0, endIndex + 1)));
            results.Add((typeAfter, new ArraySegment<ExperimentalDataPoint>(buffer, endIndex + 1, count - (endIndex + 1))));
            return results;
        }

        if (endIndex == count - 1)
        {
            results.Add((typeBefore, new ArraySegment<ExperimentalDataPoint>(buffer, 0, startIndex)));
            results.Add((activeType, new ArraySegment<ExperimentalDataPoint>(buffer, startIndex, count - startIndex)));
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
            StrainRate: Math.Abs(calculatedStrainRate) > options.DerivativeTolerance ? calculatedStrainRate : 0,
            StrainAcceleration: Math.Abs(calculatedStrainAcceleration) > options.DerivativeTolerance ? calculatedStrainAcceleration : 0,
            Stress: Math.Abs(point.Stress) > options.Tolerance ? point.Stress : 0,
            StressRate: Math.Abs(calculatedStressRate) > options.DerivativeTolerance ? calculatedStressRate : 0,
            StressAcceleration: Math.Abs(calculatedStressAcceleration) > options.DerivativeTolerance ? calculatedStressAcceleration : 0
        );
    }

    private static bool ValidateStress(SegmentType segment, double stressRate, double stressAcceleration) => segment switch
    {
        SegmentType.Ramp => stressRate > 0,
        SegmentType.Relaxation => stressRate < 0 && stressAcceleration > 0,
        SegmentType.Descent => stressRate < 0,
        SegmentType.Recovery => stressRate > 0 && stressAcceleration < 0,
        _ => false
    };
}