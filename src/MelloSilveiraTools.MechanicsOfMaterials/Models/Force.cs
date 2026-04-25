namespace MelloSilveiraTools.MechanicsOfMaterials.Models
{
    /// <summary>
    /// It represents the force.
    /// </summary>
    /// <remarks>
    /// A <see cref="Force"/> is immutable after construction. The Cartesian components
    /// <see cref="X"/>, <see cref="Y"/>, <see cref="Z"/> and the magnitude
    /// <see cref="AbsoluteValue"/> are set only by the constructors and the static factory
    /// methods, all of which compute the magnitude as the Euclidean norm of the components.
    /// Operations such as <see cref="Sum"/>, <see cref="Subtract"/>, <see cref="Round"/>,
    /// <see cref="Divide"/> and <see cref="Abs"/> always return a new <see cref="Force"/>
    /// whose <see cref="AbsoluteValue"/> is recomputed from the resulting components, so the
    /// magnitude is guaranteed to stay consistent with the components.
    /// </remarks>
    public class Force
    {
        /// <summary>
        /// Creates an empty force whose components and magnitude are all zero.
        /// </summary>
        public Force() { }

        /// <summary>
        /// Creates a force from its Cartesian components and computes its magnitude (absolute value).
        /// </summary>
        /// <param name="x">Component of the force along the X axis, in N (Newton).</param>
        /// <param name="y">Component of the force along the Y axis, in N (Newton).</param>
        /// <param name="z">Component of the force along the Z axis, in N (Newton).</param>
        public Force(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
            AbsoluteValue = CalculateAbsoluteValue(x, y, z);
        }

        /// <summary>
        /// The magnitude (Euclidean norm) of the force vector, in N (Newton).
        /// </summary>
        public double AbsoluteValue { get; private set; }

        /// <summary>
        /// Component of the force along the X axis, in N (Newton).
        /// </summary>
        public double X { get; private set; }

        /// <summary>
        /// Component of the force along the Y axis, in N (Newton).
        /// </summary>
        public double Y { get; private set; }

        /// <summary>
        /// Component of the force along the Z axis, in N (Newton).
        /// </summary>
        public double Z { get; private set; }

        /// <summary>
        /// Rounds every component of this <see cref="Force"/> to a specified
        /// number of fractional digits using banker's rounding (midpoints go to the nearest even number).
        /// The magnitude of the resulting force is recomputed from the rounded components so it stays consistent.
        /// </summary>
        /// <param name="decimals">Number of fractional digits to keep in each component.</param>
        /// <returns>A new <see cref="Force"/> with every component rounded.</returns>
        public Force Round(int decimals)
        {
            return new Force(
                Math.Round(X, decimals),
                Math.Round(Y, decimals),
                Math.Round(Z, decimals));
        }

        /// <summary>
        /// Performs the vector addition between this force and the supplied one.
        /// </summary>
        /// <param name="force">The force to be added component-wise to this instance.</param>
        /// <returns>A new <see cref="Force"/> equal to the vector sum, with its magnitude recomputed.</returns>
        public Force Sum(Force force)
        {
            return new Force(
                X + force.X,
                Y + force.Y,
                Z + force.Z);
        }

        /// <summary>
        /// Performs the vector subtraction of the supplied force from this one.
        /// </summary>
        /// <param name="force">The force to subtract component-wise from this instance.</param>
        /// <returns>A new <see cref="Force"/> equal to the vector difference, with its magnitude recomputed.</returns>
        public Force Subtract(Force force)
        {
            return new Force(
                X - force.X,
                Y - force.Y,
                Z - force.Z);
        }

        /// <summary>
        /// Divides every component of this force by a scalar value. Useful, for
        /// instance, to split a resultant load evenly among several supports. The magnitude
        /// of the resulting force is recomputed from the divided components.
        /// </summary>
        /// <param name="value">The scalar divisor.</param>
        /// <returns>A new <see cref="Force"/> whose components have been divided by <paramref name="value"/>.</returns>
        public Force Divide(int value)
        {
            return new Force(X / value, Y / value, Z / value);
        }

        /// <summary>
        /// Returns a new force whose Cartesian components are the absolute value of this force's
        /// components. The magnitude is recomputed (and equals the original magnitude, since it is
        /// already non-negative and depends only on squared components).
        /// </summary>
        /// <returns>A <see cref="Force"/> with non-negative X, Y and Z components.</returns>
        public Force Abs()
        {
            return new Force(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        }

        /// <summary>
        /// Creates a <see cref="Force"/> from its magnitude and a unit direction vector. Each
        /// Cartesian component is computed as the magnitude multiplied by the corresponding component
        /// of the normalized direction. The resulting <see cref="AbsoluteValue"/> is recomputed from
        /// the components, which equals <paramref name="absoluteValue"/> when
        /// <paramref name="normalizedDirection"/> is truly a unit vector.
        /// </summary>
        /// <param name="absoluteValue">The force magnitude in N (Newton).</param>
        /// <param name="normalizedDirection">The unit direction vector along which the force acts.</param>
        /// <returns>A new <see cref="Force"/> with the requested magnitude and direction.</returns>
        public static Force Create(double absoluteValue, Vector3D normalizedDirection)
        {
            return new Force(
                absoluteValue * normalizedDirection.X,
                absoluteValue * normalizedDirection.Y,
                absoluteValue * normalizedDirection.Z);
        }

        /// <summary>
        /// Parses a comma-separated string of three numeric values into a <see cref="Force"/>,
        /// setting the magnitude equal to the Euclidean length of the parsed vector.
        /// </summary>
        /// <param name="force">String in the form "x,y,z" containing the three force components.</param>
        /// <returns>The <see cref="Force"/> represented by the string.</returns>
        public static Force Create(string force)
        {
            var vector3D = Vector3D.Create(force);

            return new Force(vector3D.X, vector3D.Y, vector3D.Z);
        }

        /// <summary>
        /// Calculates the absolute value (Euclidean norm) from the supplied components.
        /// </summary>
        /// <param name="x">Component along the X axis.</param>
        /// <param name="y">Component along the Y axis.</param>
        /// <param name="z">Component along the Z axis.</param>
        /// <returns>The Euclidean norm of <c>(x, y, z)</c>.</returns>
        private static double CalculateAbsoluteValue(double x, double y, double z)
        {
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
        }
    }
}
