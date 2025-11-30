using MelloSilveiraTools.Domain.Models;
using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Models.NumericalMethods;

namespace MelloSilveiraTools.Domain.NumericalMethods.DifferentialEquation;

/// <summary>
/// Execute the Newmark-Beta numerical method to solve Differential Equation.
/// </summary>
public class NewmarkBetaMethod : IDifferentialEquationMethod
{
    private const double Gama = (double)1 / 2;
    private const double Beta = (double)1 / 6;
    private const double A3 = Gama / Beta;
    private const double A4 = 1 / (2 * Beta);

    public DifferentialEquationMethodType Type => DifferentialEquationMethodType.NewmarkBeta;

    /// <inheritdoc/>
    public NumericalMethodResult CalculateResult(NumericalMethodInput input, double time, NumericalMethodResult previousResult)
    {
        if (time < 0)
            throw new ArgumentOutOfRangeException(nameof(time), "The time cannot be negative.");

        if (time == 0)
            return new NumericalMethodResult { EquivalentForce = input.EquivalentForce };

        #region Step 1 - Calculates the inversed equivalent stiffness and equivalent force.
        double[,] inversedEquivalentStiffness = CalculateEquivalentStiffness(input).InverseMatrix();
        double[] equivalentForce = CalculateEquivalentForce(input, previousResult);
        #endregion

        #region Step 2 - Calculates the displacement.
        double[] deltaDisplacement = inversedEquivalentStiffness.Multiply(equivalentForce);
        double[] displacement = previousResult.Displacement.Sum(deltaDisplacement);
        #endregion

        #region Step 3 - Calculates the velocity.
        double[] velocity = new double[input.NumberOfBoundaryConditions];
        for (int i = 0; i < input.NumberOfBoundaryConditions; i++)
        {
            velocity[i] = GetA1(input.TimeStep) * deltaDisplacement[i] + (1 - A3) * previousResult.Velocity[i] - GetA5(input.TimeStep) * previousResult.Acceleration[i];
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
    /// Calculates the equivalent stiffness to calculate the displacement in Newmark-Beta method.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
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
    /// Calculates the equivalent force to calculate the displacement in Newmark-Beta method.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="previousResult"></param>
    /// <returns></returns>
    private double[] CalculateEquivalentForce(NumericalMethodInput input, NumericalMethodResult previousResult)
    {
        #region Calculates the equivalent damping and equivalent mass.
        double[,] equivalentDamping = CalculateEquivalentDamping(input);
        double[,] equivalentMass = CalculateEquivalentMass(input);
        #endregion

        #region Calculates the equivalent forces.
        double[] equivalentDampingForce = equivalentDamping.Multiply(previousResult.Velocity);
        double[] equivalentDynamicForce = equivalentMass.Multiply(previousResult.Acceleration);
        #endregion

        return input.EquivalentForce
            .Subtract(previousResult.EquivalentForce)
            .Sum(equivalentDampingForce, equivalentDynamicForce);
    }

    /// <summary>
    /// Calculates the equivalent damping to be used in Newmark-Beta method.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
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
    /// Calculates the equivalent mass to be used in Newmark-Beta method.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
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
