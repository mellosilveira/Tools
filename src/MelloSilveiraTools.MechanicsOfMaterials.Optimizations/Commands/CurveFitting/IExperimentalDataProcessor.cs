using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
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
    double StressAcceleration,
    SegmentType SegmentType)
{
    public static implicit operator ExperimentalDataPoint(ProcessedDataPoint point) => new(point.Time, point.Strain, point.Stress);
}

public record ExperimentalDataProcessingOptions(
    double StartTimeThreshold,
    ushort BufferSize = 10,
    double RelativeTimeTolerance = MathematicConstants.RelativeTolerance,
    double StrainTolerance = MathematicConstants.Tolerance,
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
                if (time.RelativeAbsolutDifference(stressTime) > options.RelativeTimeTolerance)
                {
                    logger.LogTrace("Skipping point at StrainTime={StrainTime} and StressTime={StressTime} due to time mismatch.", time, stressTime);
                    continue;
                }

                // Time Normalization
                firstValidTime ??= time;
                double normalizedTime = time - firstValidTime.Value;

                if (strain <= options.StrainTolerance)
                {
                    logger.LogTrace("Skipping point at StrainTime={StrainTime} and Strain={Strain} due to non-positive strain.", time, strain);
                    previousPoint = new(normalizedTime, strain, StrainRate: 0, StrainAcceleration: 0, stress, StressRate: 0, StressAcceleration: 0, SegmentType.Unknown);
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

    public Dictionary<SegmentType, int[]> DetermineSegmentType2(
        SegmentType currentType,
        List<ExperimentalDataPoint> buffer,
        ProcessedDataPoint basePoint,
        ExperimentalDataProcessingOptions options)
    {
        int minStrainIndex = -1, maxStrainIndex = -1, minStressIndex = -1, maxStressIndex = -1;
        double minStrain = basePoint.Strain;
        double maxStrain = basePoint.Strain;
        double minStress = basePoint.Stress;
        double maxStress = basePoint.Stress;

        for (int i = 0; i < buffer.Count; i++)
        {
            var point = buffer[i];
            if (point.Strain > maxStrain)
            {
                maxStrain = point.Strain;
                maxStrainIndex = i;
            }

            if (point.Strain < minStrain)
            {
                minStrain = point.Strain;
                minStrainIndex = i;
            }

            if (point.Stress > maxStress)
            {
                maxStress = point.Stress;
                maxStressIndex = i;
            }

            if (point.Stress < minStress)
            {
                minStress = point.Stress;
                minStressIndex = i;
            }
        }

        double strainRate = differentiation.Calculate(maxStrain, minStrain, buffer[maxStrainIndex].Time - buffer[minStrainIndex].Time);

        if (Math.Abs(strainRate) <= options.StrainTolerance)
        {
            SegmentType segmentType = (currentType == SegmentType.Descent || currentType == SegmentType.Recovery) ? SegmentType.Recovery : SegmentType.Relaxation;
            return new Dictionary<SegmentType, int[]> { { segmentType, [.. Enumerable.Range(0, buffer.Count).ToArray()] } };
        }

        if (strainRate > options.StrainTolerance)
        {
            if (minStrainIndex == 0 && maxStrainIndex == buffer.Count - 1)
                return new Dictionary<SegmentType, int[]> { { SegmentType.Ramp, [.. Enumerable.Range(0, buffer.Count).ToArray()] } };


        }

        return null;
    }

    public Dictionary<SegmentType, int[]> DetermineSegmentType(
        SegmentType currentType,
        List<ExperimentalDataPoint> buffer,
        ProcessedDataPoint basePoint,
        ExperimentalDataProcessingOptions options)
    {
        var groupedIndices = new Dictionary<SegmentType, List<int>>();

        // 1. Procurar os valores maximos e minimos de tensão e deformação (Requisito estrito)
        double maxStrain = double.MinValue, minStrain = double.MaxValue;
        double maxStress = double.MinValue, minStress = double.MaxValue;

        // Rastreamos o primeiro e o último índice de ocorrência para identificar "platôs" (constantes)
        int maxStrainIdxFirst = -1, maxStrainIdxLast = -1;
        int minStrainIdxFirst = -1, minStrainIdxLast = -1;
        int maxStressIdx = -1, minStressIdx = -1;

        for (int i = 0; i < buffer.Count; i++)
        {
            // Deformação (Strain) - Análise com Tolerância
            if (buffer[i].Strain > maxStrain)
            {
                maxStrain = buffer[i].Strain;
                maxStrainIdxFirst = i;
                maxStrainIdxLast = i;
            }
            else if (Math.Abs(buffer[i].Strain - maxStrain) <= options.StrainTolerance)
            {
                maxStrainIdxLast = i;
            }

            if (buffer[i].Strain < minStrain)
            {
                minStrain = buffer[i].Strain;
                minStrainIdxFirst = i;
                minStrainIdxLast = i;
            }
            else if (Math.Abs(buffer[i].Strain - minStrain) <= options.StrainTolerance)
            {
                minStrainIdxLast = i;
            }

            // Tensão (Stress) - Captura exigida para validações termodinâmicas futuras
            if (buffer[i].Stress > maxStress) { maxStress = buffer[i].Stress; maxStressIdx = i; }
            if (buffer[i].Stress < minStress) { minStress = buffer[i].Stress; minStressIdx = i; }
        }

        double strainDiff = maxStrain - minStrain;
        SegmentType activeType = currentType;

        // Análise 1: Deformação se mantém constante para todos os pontos no buffer
        if (strainDiff <= options.StrainTolerance)
        {
            activeType = (currentType == SegmentType.Descent || currentType == SegmentType.Recovery) ? SegmentType.Recovery : SegmentType.Relaxation;
            return new Dictionary<SegmentType, int[]> { { activeType, [.. Enumerable.Range(0, buffer.Count)] } };
        }

        // Análise 2: Variação de deformação (Rampa, Descida e Platôs intermediários)
        if (maxStrainIdxFirst >= minStrainIdxFirst)
        {
            // Tendência Geral: Subida (Rampa)
            for (int i = 0; i < buffer.Count; i++)
            {
                if (i <= minStrainIdxLast && minStrainIdxLast < maxStrainIdxFirst)
                {
                    // Fase A: Piso constante ANTES da subida
                    // Se minStrainIdxLast for 0, é apenas o ponto de partida da rampa (não é um platô real), 
                    // a menos que o estado anterior já fosse um platô.
                    if (minStrainIdxLast <= options.StrainTolerance && currentType != SegmentType.Relaxation && currentType != SegmentType.Recovery)
                    {
                        activeType = SegmentType.Ramp;
                    }
                    else
                    {
                        activeType = (currentType == SegmentType.Descent || currentType == SegmentType.Recovery) ? SegmentType.Recovery : SegmentType.Relaxation;
                    }
                }
                else if (i <= maxStrainIdxFirst)
                {
                    // Fase B: Rampa ativa ("aumenta até o ponto X")
                    activeType = SegmentType.Ramp;
                }
                else
                {
                    // Fase C: Teto constante após subida ("depois se mantém constante")
                    activeType = SegmentType.Relaxation;
                }

                if (!groupedIndices.TryGetValue(activeType, out var indexes))
                    groupedIndices[activeType] = [];

                groupedIndices[activeType].Add(i);
            }
        }
        else
        {
            // Tendência Geral: Descida (Descent)
            for (int i = 0; i < buffer.Count; i++)
            {
                if (i <= maxStrainIdxLast && maxStrainIdxLast < minStrainIdxFirst)
                {
                    // Fase A: Teto constante ANTES da descida
                    if (maxStrainIdxLast == 0 && currentType != SegmentType.Relaxation)
                    {
                        activeType = SegmentType.Descent;
                    }
                    else
                    {
                        activeType = SegmentType.Relaxation;
                    }
                }
                else if (i <= minStrainIdxFirst)
                {
                    // Fase B: Descida ativa ("diminui até o ponto X")
                    activeType = SegmentType.Descent;
                }
                else
                {
                    // Fase C: Piso constante após descida ("depois se mantém constante")
                    activeType = SegmentType.Recovery;
                }

                if (!groupedIndices.TryGetValue(activeType, out var indices))
                {
                    groupedIndices[activeType] = new List<int>();
                }
                groupedIndices[activeType].Add(i);
            }
        }

        // Mapeia as listas internas para arrays de inteiros garantindo a assinatura estrita do método
        return groupedIndices.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }
}