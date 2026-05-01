using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.LoadSharing;

// TODO: Relações entre deslocamento do tecido e deslocamento do sistema tem um + ou -, estudar se vale a pena incluir porque a cada
// vez que calcular vai precisar checar se o valor é valido perante algumas premissas, por exemplo: angulo não deve mudar de quadrante, 
// deformação não pode ser maior que o tamanho do tecido.

/// <summary>
/// Performs calculations for load sharing considering a 1D specimen in three-dimensional space.
/// </summary>
public class LoadSharing1DSpecimenThreeDimensionalSpaceCalculator : ILoadSharingCalculator
{
    /// <inheritdoc/>
    public MechanicalParameter CreateSpecimenDisplacement(MechanicalParameter systemDisplacement, SpecimenParameter specimenParameter)
    {
        Expression? systemDisplacementExpression = systemDisplacement?.Expression;
        double? initialVariableValue = systemDisplacementExpression?.InitialVariableValue;
        double? finalVariableValue = systemDisplacementExpression?.FinalVariableValue;
        var specimenDisplacementFunction = new GenericFunction
        (
            initialVariableValue,
            finalVariableValue,
            function: time => CalculateSpecimenDisplacement(systemDisplacement, specimenParameter, time),
            derivativeFunction: time => CalculateSpecimenDisplacementDerivative(systemDisplacement, specimenParameter, time),
            integralFunction: null
        );

        return new MechanicalParameter
        (
            CalculateSpecimenDisplacement(specimenParameter, systemDisplacement?.InitialValue ?? 0),
            new Expression(initialVariableValue, finalVariableValue, [specimenDisplacementFunction])
        );
    }

    /// <inheritdoc/>
    public double CalculateSystemDisplacement(SpecimenParameter specimenParameter, double specimenDisplacement)
    {
        double initialZAngle = specimenParameter.InitialAngle.Z;

        if (specimenParameter.ConsiderAngleVariation)
        {
            double initialLength = specimenParameter.InitialLength;
            double preloadLength = specimenParameter.PreLoadLength;
            return Math.Sqrt((initialLength + specimenDisplacement).Squared() - (preloadLength * Math.Sin(initialZAngle)).Squared()) 
                - preloadLength * Math.Cos(initialZAngle);
        }

        return specimenDisplacement * Math.Cos(initialZAngle);
    }

    /// <inheritdoc/>
    public Vector3D CalculateSpecimenAngle(SpecimenParameter specimenParameter, double systemDisplacement)
    {
        double initialZAngle = specimenParameter.InitialAngle.Z;

        double zAngle;
        if (specimenParameter.ConsiderAngleVariation)
        {
            double preloadLength = specimenParameter.PreLoadLength;
            zAngle = Math.Atan(
                preloadLength * Math.Sin(initialZAngle)
                / (preloadLength * Math.Cos(initialZAngle) + systemDisplacement));
        }
        else
        {
            zAngle = initialZAngle;
        }

        return Vector3D.Create(0, 0, zAngle);
    }

    /// <inheritdoc/>
    public double CalculateSpecimenDisplacement(SpecimenParameter specimenParameter, double systemDisplacement)
    {
        double initialZAngle = specimenParameter.InitialAngle.Z;

        if (specimenParameter.ConsiderAngleVariation)
        {
            double initialLength = specimenParameter.InitialLength;
            double preloadLength = specimenParameter.PreLoadLength;
            return Math.Sqrt(
                    systemDisplacement.Squared()
                    + preloadLength.Squared()
                    + 2 * systemDisplacement * preloadLength * Math.Cos(initialZAngle))
                - initialLength;
        }

        return systemDisplacement / Math.Cos(initialZAngle);
    }

    /// <inheritdoc/>
    public double CalculateSpecimenDisplacement(MechanicalParameter systemDisplacement, SpecimenParameter specimenParameter, double time)
    {
        return CalculateSpecimenDisplacement(specimenParameter, systemDisplacement.CalculateValue(time));
    }

    /// <inheritdoc/>
    public double CalculateSpecimenDisplacementDerivative(SpecimenParameter specimenParameter, double systemDisplacementDerivative, double systemDisplacement)
    {
        double initialZAngle = specimenParameter.InitialAngle.Z;

        if (specimenParameter.ConsiderAngleVariation)
        {
            double preloadLength = specimenParameter.PreLoadLength;
            double cosAngle = Math.Cos(initialZAngle);
            return systemDisplacementDerivative * (preloadLength * cosAngle + systemDisplacement)
                / Math.Sqrt(
                    systemDisplacement.Squared()
                    + preloadLength.Squared()
                    + 2 * systemDisplacement * preloadLength * cosAngle);
        }

        return systemDisplacementDerivative / Math.Cos(initialZAngle);
    }

    /// <inheritdoc/>
    public double CalculateSpecimenDisplacementDerivative(MechanicalParameter systemDisplacement, SpecimenParameter specimenParameter, double time)
    {
        (double displacement, double displacementDerivative) = systemDisplacement.CalculateValueAndDerivative(time);
        return CalculateSpecimenDisplacementDerivative(specimenParameter, displacementDerivative, displacement);
    }

    /// <inheritdoc/>
    public (Vector3D Angle, double Displacement) CalculateSpecimenAngleAndDisplacement(SpecimenParameter specimenParameter, double systemDisplacement)
    {
        return (
            CalculateSpecimenAngle(specimenParameter, systemDisplacement),
            CalculateSpecimenDisplacement(specimenParameter, systemDisplacement)
        );
    }

    /// <inheritdoc/>
    public (double Displacement, double DisplacementDerivative) CalculateSpecimenDisplacementAndDerivative(MechanicalParameter systemDisplacement, SpecimenParameter specimenParameter, double time)
    {
        return (
            CalculateSpecimenDisplacement(systemDisplacement, specimenParameter, time),
            CalculateSpecimenDisplacementDerivative(systemDisplacement, specimenParameter, time)
        );
    }

    /// <inheritdoc/>
    public double CalculateSpecimenForceOnSystemAxis(Vector3D angle, double force)
    {
        return force * Math.Cos(angle.Z);
    }
}
