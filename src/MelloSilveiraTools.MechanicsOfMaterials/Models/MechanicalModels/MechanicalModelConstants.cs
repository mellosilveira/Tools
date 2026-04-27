namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Contain the constats used in application.
/// </summary>
public static class MechanicalModelConstants
{
    /// <summary>
    /// Unit: s (second).
    /// </summary>
    public const double InitialTime = 0;

    /// <summary>
    /// The constant of Euler-Mascheroni.
    /// </summary>
    public const double EulerMascheroniConstant = 0.5772156649015328606065120900824024310421;

    /// <summary>
    /// The tolerance for time comparisons.
    /// Used to distinguish "effectively zero" from any meaningful positive value.
    /// Chosen as 1e-10 s (0.1 nanosecond), well below any physically relevant time in the simulation.
    /// </summary>
    public const double Tolerance = 1e-10;

    /// <summary>
    /// The relative double-point precision accepted for value equality comparisons.
    /// Two values whose relative difference is less than this threshold are considered equal.
    /// </summary>
    public const double RelativeTolerance = 1e-8;

    /// <summary>
    /// The final time to equation E1 must be at most 11.4, because, using Geogebra to plot the graphic for the 
    /// equation e^(-t)/t, we found to that time a value less than the precision assumed to the project.
    /// f(t) = e^(-t)/t
    /// f(11.4) = 9.820601e-7
    /// f(11.4) less than tolerance = 1e-6
    /// </summary>
    // TODO: MUDAR PARA CALCULAR AO INVÉS DE SETAR O VALOR.
    public const double EquationE1MaximumFinalTime = 11.4;
}
