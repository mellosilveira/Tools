namespace MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear;

/// <summary>
/// Contains the range of values accepted for a variable.
/// </summary>
/// <param name="InitialPoint">Unit depends on which variable the accepted range is applied.</param>
/// <param name="FinalPoint">Unit depends on which variable the accepted range is applied.</param>
public sealed record AcceptedRange(double InitialPoint, double FinalPoint);
