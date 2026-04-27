using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear;

/// <summary>
/// A linear viscoelastic model. Establish a linear stress-strain relation that depends only on time.
/// For more details, see on section "Bibliographies" on file "README.MD".
/// </summary>
/// <typeparam name="TInput">Type of linear viscoelastic model's input.</typeparam>
public interface ILinearModelCalculator<TInput> : IViscoelasticModelCalculator<TInput> where TInput : MechanicalModelInput, new()
{ }
