using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;

/// <summary>
/// Fung's quasi-linear viscoelastic model.
/// </summary>
public interface IFungModelCalculator : IQuasiLinearModelCalculator<FungModelInput, ReducedRelaxationFunction>
{
    /// <summary>
    /// Calculates the equation I(t) where t is the time.
    /// </summary>
    /// <param name="slowRelaxationTime">Unit: s (second).</param>
    /// <param name="fastRelaxationTime">Unit: s (second).</param>
    /// <param name="timeStep">Unit: s (second).</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateI(double slowRelaxationTime, double fastRelaxationTime, double timeStep, double time);
}
