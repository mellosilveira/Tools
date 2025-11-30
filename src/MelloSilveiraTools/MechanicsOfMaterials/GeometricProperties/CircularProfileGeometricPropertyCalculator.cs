using MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;

namespace MelloSilveiraTools.MechanicsOfMaterials.GeometricProperties
{
    /// <summary>
    /// It is responsible to calculate the geometric properties to circular profile.
    /// </summary>
    public class CircularProfileGeometricPropertyCalculator : IGeometricPropertyCalculator<CircularProfile>
    {
        /// <inheritdoc/>
        public double CalculateArea(CircularProfile profile)
        {
            return profile.Thickness.HasValue ?
                Math.PI / 4 * (Math.Pow(profile.Diameter, 2) - Math.Pow(profile.Diameter - 2 * profile.Thickness.Value, 2)) 
                : Math.PI / 4 * Math.Pow(profile.Diameter, 2);
        }

        /// <inheritdoc/>
        public double CalculateMomentOfInertia(CircularProfile profile)
        {
            return profile.Thickness.HasValue ?
                Math.PI / 64 * (Math.Pow(profile.Diameter, 4) - Math.Pow(profile.Diameter - 2 * profile.Thickness.Value, 4))
                : Math.PI / 64 * Math.Pow(profile.Diameter, 4);
        }
    }
}
