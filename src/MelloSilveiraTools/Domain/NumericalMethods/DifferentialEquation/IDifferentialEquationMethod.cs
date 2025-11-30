using MelloSilveiraTools.Domain.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.NumericalMethods;

namespace MelloSilveiraTools.Domain.NumericalMethods.DifferentialEquation;

/// <summary>
/// Executes numerical method to solve Differential Equation
/// </summary>
public interface IDifferentialEquationMethod
{
    DifferentialEquationMethodType Type { get; } 

    /// <summary>
    /// Calculates the results for a numeric analysis.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="time"></param>
    /// <param name="previousResult"></param>
    /// <returns></returns>
    NumericalMethodResult CalculateResult(NumericalMethodInput input, double time, NumericalMethodResult previousResult);
}