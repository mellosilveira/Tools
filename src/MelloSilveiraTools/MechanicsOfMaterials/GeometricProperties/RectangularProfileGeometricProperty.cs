using MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;

namespace MelloSilveiraTools.MechanicsOfMaterials.GeometricProperties
{
    /// <summary>
    /// It is responsible to calculate the geometric properties to rectangular profile.
    /// </summary>
    public class RectangularProfileGeometricProperty : IGeometricProperty<RectangularProfile>
    {
        /// <inheritdoc/>
        public double CalculateArea(RectangularProfile profile)
        {
            return profile.Thickness.HasValue ?
                profile.Width * profile.Height - (profile.Width - 2 * profile.Thickness.Value) * (profile.Height - 2 * profile.Thickness.Value)
                : profile.Width * profile.Height;
        }

        /// <inheritdoc/>
        public double CalculateMomentOfInertia(RectangularProfile profile)
        {
            return profile.Thickness.HasValue ?
                (Math.Pow(profile.Height, 3) * profile.Width - Math.Pow(profile.Height - 2 * profile.Thickness.Value, 3) * (profile.Width - 2 * profile.Thickness.Value)) / 12
                : Math.Pow(profile.Height, 3) * profile.Width / 12;
        }
    }
}
