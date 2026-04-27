using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Physics
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
    public record Force
    {
        private Force(Vector3D vector) : this(vector.X, vector.Y, vector.Z) { }

        /// <summary>
        /// Creates a force from its Cartesian components and computes its magnitude (absolute value).
        /// </summary>
        /// <param name="x">Component of the force along the X axis, in N (Newton).</param>
        /// <param name="y">Component of the force along the Y axis, in N (Newton).</param>
        /// <param name="z">Component of the force along the Z axis, in N (Newton).</param>
        private Force(double x, double y, double z)
        {
            var vector = Vector3D.Create(x, y, z);
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
            AbsoluteValue = vector.Length;
        }

        /// <summary>
        /// Component of the force along the X axis, in N (Newton).
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Component of the force along the Y axis, in N (Newton).
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Component of the force along the Z axis, in N (Newton).
        /// </summary>
        public double Z { get; }

        /// <summary>
        /// The magnitude (Euclidean norm) of the force vector, in N (Newton).
        /// </summary>
        public double AbsoluteValue { get; }

        /// <summary>
        /// Rounds every component of this <see cref="Force"/> to a specified
        /// number of fractional digits using banker's rounding (midpoints go to the nearest even number).
        /// The magnitude of the resulting force is recomputed from the rounded components so it stays consistent.
        /// </summary>
        /// <param name="decimals">Number of fractional digits to keep in each component.</param>
        /// <returns>A new <see cref="Force"/> with every component rounded.</returns>
        public Force Round(int decimals) => new(Math.Round(X, decimals), Math.Round(Y, decimals), Math.Round(Z, decimals));

        /// <summary>
        /// Performs the vector addition between this force and the supplied one.
        /// </summary>
        /// <param name="force">The force to be added component-wise to this instance.</param>
        /// <returns>A new <see cref="Force"/> equal to the vector sum, with its magnitude recomputed.</returns>
        public Force Sum(Force force) => new(X + force.X, Y + force.Y, Z + force.Z);

        /// <summary>
        /// Performs the vector subtraction of the supplied force from this one.
        /// </summary>
        /// <param name="force">The force to subtract component-wise from this instance.</param>
        /// <returns>A new <see cref="Force"/> equal to the vector difference, with its magnitude recomputed.</returns>
        public Force Subtract(Force force) => new(X - force.X, Y - force.Y, Z - force.Z);

        /// <summary>
        /// Divides every component of this force by a scalar value. Useful, for
        /// instance, to split a resultant load evenly among several supports. The magnitude
        /// of the resulting force is recomputed from the divided components.
        /// </summary>
        /// <param name="value">The scalar divisor.</param>
        /// <returns>A new <see cref="Force"/> whose components have been divided by <paramref name="value"/>.</returns>
        public Force Divide(int value) => new(X / value, Y / value, Z / value);

        /// <summary>
        /// Returns a new force whose Cartesian components are the absolute value of this force's
        /// components. The magnitude is recomputed (and equals the original magnitude, since it is
        /// already non-negative and depends only on squared components).
        /// </summary>
        /// <returns>A <see cref="Force"/> with non-negative X, Y and Z components.</returns>
        public Force Abs() => new(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));

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
        public static Force Create(double absoluteValue, Vector3D normalizedDirection) => new(normalizedDirection * absoluteValue);

        /// <summary>
        /// Parses a comma-separated string of three numeric values into a <see cref="Force"/>,
        /// setting the magnitude equal to the Euclidean length of the parsed vector.
        /// </summary>
        /// <param name="force">String in the form "x,y,z" containing the three force components.</param>
        /// <returns>The <see cref="Force"/> represented by the string.</returns>
        public static Force Create(string force) => new(Vector3D.Parse(force));
    }
}
