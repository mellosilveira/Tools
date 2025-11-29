using MelloSilveiraTools.MechanicsOfMaterials.Models.Enums;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models;

/// <summary>
/// Contains the necessary information about each material that could be used in project.
/// </summary>
/// <param name="YoungModulus">Unit: MPa (Pascal).</param>
/// <param name="YieldStrength">Unit: MPa (Pascal).</param>
/// <param name="TensileStress">Unit: MPa (Mega Pascal).</param>
/// <param name="SpecificMass">Unit: kg/m³ (kilogram per cubic meters).</param>
public readonly record struct Material(double YoungModulus, double YieldStrength, double TensileStress, double SpecificMass)
{
    /// <summary>
    /// It contains the necessary information about Steel SAE 1020.
    /// </summary>
    public static readonly Material Steel1020 = new(205e3, 350, 470, 7850);

    /// <summary>
    /// It contains the necessary information about Steel SAE 1045.
    /// </summary>
    public static readonly Material Steel1045 = new(200e3, 450, 738, 7850);

    /// <summary>
    /// It contains the necessary information about Steel SAE 4130.
    /// </summary>
    public static readonly Material Steel4130 = new(200e3, 552, 860, 7850);

    /// <summary>
    /// It contains the necessary information about Aluminum 6061-T6.
    /// </summary>
    public static readonly Material Aluminum6061T6 = new(70e3, 310, 290, 2710);

    /// <summary>
    /// Creates an instance of class <seealso cref="Material"/>.
    /// </summary>
    /// <param name="materialType"></param>
    /// <returns></returns>
    public static Material Create(MaterialType materialType) => materialType switch
    {
        MaterialType.Steel1020 => Steel1020,
        MaterialType.Steel1045 => Steel1045,
        MaterialType.Steel4130 => Steel4130,
        MaterialType.Aluminum6061T6 => Aluminum6061T6,
        _ => throw new ArgumentOutOfRangeException(nameof(materialType))
    };
}
