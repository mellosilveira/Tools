using System.Text.Json.Serialization;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;

/// <summary>
/// It represents the generic profile.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CircularProfile), "Circular")]
[JsonDerivedType(typeof(RectangularProfile), "Rectangular")]
public abstract class Profile
{
    /// <summary>
    /// The thickness.
    /// Unit: mm (milimeter).
    /// </summary>
    public double? Thickness { get; set; }
}
