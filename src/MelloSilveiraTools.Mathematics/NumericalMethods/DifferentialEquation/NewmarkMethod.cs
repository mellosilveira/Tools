using MelloSilveiraTools.Core.Domain.Models;
using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.DifferentialEquation;

/// <summary>
/// Execute the Newmark numerical method to solve Differential Equation.
/// </summary>
public class NewmarkMethod : IDifferentialEquationMethod
{
    private const double Gama = (double)1 / 2;
    private const double Beta = (double)1 / 4;

    /// <inheritdoc/>
    public DifferentialEquationMethodType Type => DifferentialEquationMethodType.Newmark;
    
    /// <inheritdoc/>
    public NumericalMethodResult CalculateResult(NumericalMethodInput input, double time, NumericalMethodResult previousResult)
    {
        if (time < Constants.InitialTime)
            throw new ArgumentOutOfRangeException(nameof(time), $"The time cannot be less than the initial time: {Constants.InitialTime}.");

        if (time == Constants.InitialTime)
            return new NumericalMethodResult { EquivalentForce = input.EquivalentForce };

        #region Step 1 - Calculates the equivalent stiffness and equivalent force.
        double[,] inversedEquivalentStiffness = CalculateEquivalentStiffness(input).InverseMatrix();
        double[] equivalentForce = CalculateEquivalentForce(input, previousResult.Displacement, previousResult.Velocity, previousResult.Acceleration);
        #endregion

        #region Step 2 - Calculates the displacement.
        double[] displacement = inversedEquivalentStiffness.Multiply(equivalentForce);
        #endregion

        #region Step 3 - Calculates the velocity and acceleration.
        double[] velocity = new double[input.NumberOfBoundaryConditions];
        double[] acceleration = new double[input.NumberOfBoundaryConditions];
        for (int i = 0; i < input.NumberOfBoundaryConditions; i++)
        {
            acceleration[i] = GetA0(input.TimeStep) * (displacement[i] - previousResult.Displacement[i]) - GetA2(input.TimeStep) * previousResult.Velocity[i] - GetA3() * previousResult.Acceleration[i];
            velocity[i] = previousResult.Velocity[i] + GetA6(input.TimeStep) * previousResult.Acceleration[i] + GetA7(input.TimeStep) * acceleration[i];
        }
        #endregion

        return new()
        {
            Time = time,
            Displacement = displacement,
            Velocity = velocity,
            Acceleration = acceleration,
            EquivalentForce = input.EquivalentForce
        };
    }

    /// <summary>
    /// Builds the effective stiffness matrix [K̂] = a₀[M] + a₁[C] + [K] used to solve for
    /// the displacement vector at the current step.
    /// </summary>
    /// <param name="input">System input providing the mass, damping and stiffness matrices and the time step Δt.</param>
    /// <returns>The effective stiffness matrix, with the same dimensions as the system matrices.</returns>
    private double[,] CalculateEquivalentStiffness(NumericalMethodInput input)
    {
        double[,] equivalentStiffness = new double[input.NumberOfBoundaryConditions, input.NumberOfBoundaryConditions];
        for (int i = 0; i < input.NumberOfBoundaryConditions; i++)
        {
            for (int j = 0; j < input.NumberOfBoundaryConditions; j++)
            {
                equivalentStiffness[i, j] = GetA0(input.TimeStep) * input.Mass[i, j] + GetA1(input.TimeStep) * input.Damping[i, j] + input.Stiffness[i, j];
            }
        }

        return equivalentStiffness;
    }

    /// <summary>
    /// Builds the effective force vector F̂ = F + [C]·v_eq + [M]·a_eq, where v_eq and a_eq are
    /// the equivalent velocity and acceleration extrapolated from the previous step. The result
    /// is used together with the effective stiffness to solve for the new displacement.
    /// </summary>
    /// <param name="input">System input providing the mass and damping matrices and the current applied force.</param>
    /// <param name="previousDisplacement">Displacement vector x(t−Δt), in meters.</param>
    /// <param name="previousVelocity">Velocity vector ẋ(t−Δt), in m/s.</param>
    /// <param name="previousAcceleration">Acceleration vector ẍ(t−Δt), in m/s².</param>
    /// <returns>The effective force vector F̂ at the current step, in Newtons.</returns>
    private double[] CalculateEquivalentForce(NumericalMethodInput input, double[] previousDisplacement, double[] previousVelocity, double[] previousAcceleration)
    {
        #region Calculates the equivalent velocity and equivalent acceleration.
        double[] equivalentVelocity = CalculateEquivalentVelocity(input, previousDisplacement, previousVelocity, previousAcceleration);
        double[] equivalentAcceleration = CalculateEquivalentAcceleration(input, previousDisplacement, previousVelocity, previousAcceleration);
        #endregion

        #region Calculates the equivalent forces.
        double[] equivalentDampingForce = input.Damping.Multiply(equivalentVelocity);
        double[] equivalentDynamicForce = input.Mass.Multiply(equivalentAcceleration);
        #endregion

        return input.EquivalentForce.Sum(equivalentDampingForce, equivalentDynamicForce);
    }

    /// <summary>
    /// Computes a_eq = a₀·x(t−Δt) + a₂·ẋ(t−Δt) + a₃·ẍ(t−Δt), the equivalent acceleration term that enters the effective force vector.
    /// </summary>
    /// <param name="input">System input providing the time step Δt.</param>
    /// <param name="previousDisplacement">Displacement vector at t−Δt, in meters.</param>
    /// <param name="previousVelocity">Velocity vector at t−Δt, in m/s.</param>
    /// <param name="previousAcceleration">Acceleration vector at t−Δt, in m/s².</param>
    /// <returns>The equivalent acceleration vector used to assemble the effective force.</returns>
    private double[] CalculateEquivalentAcceleration(NumericalMethodInput input, double[] previousDisplacement, double[] previousVelocity, double[] previousAcceleration)
    {
        double[] equivalentAcceleration = new double[input.NumberOfBoundaryConditions];
        for (int i = 0; i < input.NumberOfBoundaryConditions; i++)
        {
            equivalentAcceleration[i] = GetA0(input.TimeStep) * previousDisplacement[i] + GetA2(input.TimeStep) * previousVelocity[i] + GetA3() * previousAcceleration[i];
        }

        return equivalentAcceleration;
    }

    /// <summary>
    /// Computes v_eq = a₁·x(t−Δt) + a₄·ẋ(t−Δt) + a₅·ẍ(t−Δt), the equivalent velocity term
    /// that enters the effective force vector.
    /// </summary>
    /// <param name="input">System input providing the time step Δt.</param>
    /// <param name="previousDisplacement">Displacement vector at t−Δt, in meters.</param>
    /// <param name="previousVelocity">Velocity vector at t−Δt, in m/s.</param>
    /// <param name="previousAcceleration">Acceleration vector at t−Δt, in m/s².</param>
    /// <returns>The equivalent velocity vector used to assemble the effective force.</returns>
    private double[] CalculateEquivalentVelocity(NumericalMethodInput input, double[] previousDisplacement, double[] previousVelocity, double[] previousAcceleration)
    {
        double[] equivalentVelocity = new double[input.NumberOfBoundaryConditions];
        for (int i = 0; i < input.NumberOfBoundaryConditions; i++)
        {
            equivalentVelocity[i] = GetA1(input.TimeStep) * previousDisplacement[i] + GetA4() * previousVelocity[i] + GetA5(input.TimeStep) * previousAcceleration[i];
        }

        return equivalentVelocity;
    }

    #region Integration Constants

    private double GetA0(double timeStep) => 1 / (Beta * Math.Pow(timeStep, 2));

    private double GetA1(double timeStep) => Gama / (Beta * timeStep);

    private double GetA2(double timeStep) => 1 / (Beta * timeStep);

    private double GetA3() => 1 / (2 * Beta) - 1;

    private double GetA4() => Gama / Beta - 1;

    private double GetA5(double timeStep) => timeStep / 2 * (Gama / Beta - 2);

    private double GetA6(double timeStep) => timeStep * (1 - Gama);

    private double GetA7(double timeStep) => Gama * timeStep;

    #endregion
}
