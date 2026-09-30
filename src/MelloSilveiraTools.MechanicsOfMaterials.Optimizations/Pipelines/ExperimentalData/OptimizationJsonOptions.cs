using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Converters;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Centralized JSON serialization options for optimization pipeline steps.
/// </summary>
internal static class OptimizationJsonOptions
{
    /// <summary>
    /// Shared JSON serialization options configured with <see cref="SignificantFiguresDoubleJsonConverter"/> set to 7 significant figures.
    /// </summary>
    internal static readonly JsonSerializerOptions SignificantFigures7 = new() { Converters = { new SignificantFiguresDoubleJsonConverter(7) } };
}
