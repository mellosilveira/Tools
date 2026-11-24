using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;

/// <summary>
/// Encapsulates the downloadable CSV stream (which deletes the temporary file on disposal),
/// file download metadata, simulation delta outputs, and asymptotic convergence time.
/// </summary>
/// <param name="DeltaOutput">Initial, final, absolute delta, and percentage delta simulation outputs.</param>
/// <param name="AsymptoteTime">Time (in seconds) at which asymptotic convergence was detected, or <see langword="null"/> if not reached.</param>
/// <param name="FileStream">Readable stream of the generated CSV file; automatically deletes the underlying file when disposed.</param>
/// <param name="FileName">Download file name including extension.</param>
/// <param name="ContentType">MIME content type of the generated file (e.g. <c>"text/csv"</c>).</param>
public sealed record ExecuteSpecimenAnalysisResultData(
    SimulationDeltaOutput DeltaOutput,
    double? AsymptoteTime,
    Stream FileStream,
    string FileName,
    string ContentType);
