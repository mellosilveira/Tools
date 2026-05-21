namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

/// <summary>
/// Contain the constats used in application.
/// </summary>
public static class MechanicalModelConstants
{
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
