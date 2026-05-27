using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.DifferentialEquation;

/// <summary>
/// Execute the Newmark-Beta numerical method to solve Differential Equation.
/// </summary>
public class NewmarkBetaMethod : IDifferentialEquationMethod
{
    private const double Gama = (double)1 / 2;
    private const double Beta = (double)1 / 6;
    private const double A3 = Gama / Beta;
    private const double A4 = 1 / (2 * Beta);

    /// <inheritdoc/>
    public DifferentialEquationMethodType Type => DifferentialEquationMethodType.NewmarkBeta;

    /// <inheritdoc/>
    public NumericalMethodOutput Calculate(NumericalMethodInput input, double time, NumericalMethodOutput previousOutput)
    {
        if (time < 0)
            throw new ArgumentOutOfRangeException(nameof(time), "The time cannot be negative.");

        if (time == 0)
            return new NumericalMethodOutput { EquivalentForce = input.EquivalentForce };

        #region Step 1 - Calculates the inversed equivalent stiffness and equivalent force.
        double[,] inversedEquivalentStiffness = CalculateEquivalentStiffness(input).InverseMatrix();
        double[] equivalentForce = CalculateEquivalentForce(input, previousOutput);
        #endregion

        #region Step 2 - Calculates the displacement.
        double[] deltaDisplacement = inversedEquivalentStiffness.Multiply(equivalentForce);
        double[] displacement = previousOutput.Displacement.Sum(deltaDisplacement);
        #endregion

        #region Step 3 - Calculates the velocity.
        double[] velocity = new double[input.NumberOfBoundaryConditions];
        for (int i = 0; i < input.NumberOfBoundaryConditions; i++)
        {
            velocity[i] = GetA1(input.TimeStep) * deltaDisplacement[i] + (1 - A3) * previousOutput.Velocity[i] - GetA5(input.TimeStep) * previousOutput.Acceleration[i];
        }
        #endregion

        #region Step 4 - Calculates the acceleration.
        double[] damping_velocity = input.Damping.Multiply(velocity);
        double[] stiffness_displacement = input.Stiffness.Multiply(displacement);
        double[] systemEquivalentForce = input.EquivalentForce.Subtract(damping_velocity).Subtract(stiffness_displacement);
        double[,] inversedMass = input.Mass.InverseMatrix();

        // [Acceleration] = -inv([M]) * [System Equivalent Force]
        //    [System Equivalent Force] = [Equivalent Force] - [Stiffness] * [Diplacement] - [Damping] * [Velocity]
        double[] acceleration = inversedMass.Multiply(systemEquivalentForce);

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
    /// Builds the effective stiffness matrix [K̂] = a₀[M] + a₁[C] + [K] used to solve for the
    /// displacement increment Δx at the current step.
    /// </summary>
    /// <param name="input">System input providing the mass, damping and stiffness matrices and the time step Δt.</param>
    /// <returns>The effective stiffness matrix.</returns>
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
    /// Builds the effective force vector ΔF̂ used to solve for the displacement increment in the
    /// Newmark-Beta incremental formulation. The vector combines the change in applied force
    /// (F(t) − F(t−Δt)) with the equivalent damping and equivalent mass contributions evaluated
    /// from the previous step.
    /// </summary>
    /// <param name="input">System input providing the matrices and current applied force.</param>
    /// <param name="previousOutput">State at t−Δt (displacement, velocity, acceleration and applied force).</param>
    /// <returns>The effective force vector at the current step, in Newtons.</returns>
    private double[] CalculateEquivalentForce(NumericalMethodInput input, NumericalMethodOutput previousOutput)
    {
        #region Calculates the equivalent damping and equivalent mass.
        double[,] equivalentDamping = CalculateEquivalentDamping(input);
        double[,] equivalentMass = CalculateEquivalentMass(input);
        #endregion

        #region Calculates the equivalent forces.
        double[] equivalentDampingForce = equivalentDamping.Multiply(previousOutput.Velocity);
        double[] equivalentDynamicForce = equivalentMass.Multiply(previousOutput.Acceleration);
        #endregion

        return input.EquivalentForce
            .Subtract(previousOutput.EquivalentForce)
            .Sum(equivalentDampingForce, equivalentDynamicForce);
    }

    /// <summary>
    /// Builds the equivalent damping matrix [Ĉ] = a₂[M] + A₃[C] used to multiply the previous
    /// velocity when assembling the effective force vector.
    /// </summary>
    /// <param name="input">System input providing the mass and damping matrices and the time step Δt.</param>
    /// <returns>The equivalent damping matrix.</returns>
    private double[,] CalculateEquivalentDamping(NumericalMethodInput input)
    {
        double[,] equivalentDamping = new double[input.NumberOfBoundaryConditions, input.NumberOfBoundaryConditions];
        for (int i = 0; i < input.NumberOfBoundaryConditions; i++)
        {
            for (int j = 0; j < input.NumberOfBoundaryConditions; j++)
            {
                equivalentDamping[i, j] = GetA2(input.TimeStep) * input.Mass[i, j] + A3 * input.Damping[i, j];
            }
        }

        return equivalentDamping;
    }

    /// <summary>
    /// Builds the equivalent mass matrix [M̂] = A₄[M] + a₅[C] used to multiply the previous
    /// acceleration when assembling the effective force vector.
    /// </summary>
    /// <param name="input">System input providing the mass and damping matrices and the time step Δt.</param>
    /// <returns>The equivalent mass matrix.</returns>
    private double[,] CalculateEquivalentMass(NumericalMethodInput input)
    {
        double[,] equivalentMass = new double[input.NumberOfBoundaryConditions, input.NumberOfBoundaryConditions];
        for (int i = 0; i < input.NumberOfBoundaryConditions; i++)
        {
            for (int j = 0; j < input.NumberOfBoundaryConditions; j++)
            {
                equivalentMass[i, j] = A4 * input.Mass[i, j] + GetA5(input.TimeStep) * input.Damping[i, j];
            }
        }

        return equivalentMass;
    }

    #region Integration Constants

    private double GetA0(double timeStep) => 1 / (Beta * Math.Pow(timeStep, 2));
    private double GetA1(double timeStep) => Gama / (Beta * timeStep);
    private double GetA2(double timeStep) => 1 / (Beta * timeStep);
    private double GetA5(double timeStep) => -timeStep * (1 - Gama / (2 * Beta));

    #endregion
}
