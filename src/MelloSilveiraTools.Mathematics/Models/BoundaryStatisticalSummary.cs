using MelloSilveiraTools.Mathematics.Models.Statistics;

namespace SoftTissue.DataContracts.Commands.MechanicalModels.Commons;

/// <summary>
/// Represents the consolidated statistical summary of boundary conditions and their relative variations.
/// </summary>
/// <param name="InitialValues">The statistical data computed at the initial boundary of the analysis.</param>
/// <param name="FinalValues">The statistical data computed at the final boundary of the analysis.</param>
/// <param name="Variations">The statistical data representing the relative variations between final and initial boundaries.</param>
public sealed record BoundaryStatisticalSummary(StatisticalData InitialValues, StatisticalData FinalValues, StatisticalData Variations);