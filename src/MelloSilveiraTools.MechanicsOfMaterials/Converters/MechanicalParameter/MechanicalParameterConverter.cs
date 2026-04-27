using MelloSilveiraTools.Mathematics.Converters;
using MelloSilveiraTools.MechanicsOfMaterials.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;

/// <inheritdoc cref="IMechanicalParameterConverter"/>
public class MechanicalParameterConverter : IMechanicalParameterConverter
{
    /// <inheritdoc/>
    public double CalculateDisplacementFromStrain(SpecimenParameter specimenParameter, double strain)
    {
        return specimenParameter.ConsiderLargeDisplacement
            ? specimenParameter.InitialLength * (Math.Exp(strain) - 1)
            : specimenParameter.InitialLength * strain;
    }

    /// <inheritdoc/>
    public double CalculateDisplacementDerivativeFromStrain(SpecimenParameter specimenParameter, double strain, double strainDerivative)
    {
        return specimenParameter.ConsiderLargeDisplacement
            ? specimenParameter.PreLoadLength * Math.Exp(strain) * strainDerivative
            : specimenParameter.InitialLength * strainDerivative;
    }

    /// <inheritdoc/>
    public double CalculateStrainFromDisplacement(SpecimenParameter specimenParameter, double displacement)
    {
        return specimenParameter.ConsiderLargeDisplacement
            ? Math.Log(1 + displacement / specimenParameter.InitialLength)
            : displacement / specimenParameter.InitialLength;
    }

    /// <inheritdoc/>
    public double CalculateStrainDerivativeFromDisplacement(SpecimenParameter specimenParameter, double displacement, double displacementDerivative)
    {
        return specimenParameter.ConsiderLargeDisplacement
            ? displacementDerivative / (specimenParameter.InitialLength + displacement)
            : displacementDerivative / specimenParameter.InitialLength;
    }

    /// <inheritdoc/>
    public double CalculatePreloadedDisplacementFromStrain(SpecimenParameter specimenParameter, double strain)
    {
        return specimenParameter.ConsiderLargeDisplacement
            ? specimenParameter.InitialLength * (specimenParameter.PreLoadFactor * Math.Exp(strain) - 1)
            : specimenParameter.InitialLength * (specimenParameter.PreLoadFactor * (strain + 1) - 1);
    }

    /// <inheritdoc/>
    public double CalculatePreloadedDisplacementDerivativeFromStrain(SpecimenParameter specimenParameter, double strain, double strainDerivative)
    {
        throw new NotImplementedException($"The method '{nameof(CalculatePreloadedDisplacementDerivativeFromStrain)}' was not implemented.");
    }

    /// <inheritdoc/>
    public double CalculatePreloadedStrainFromDisplacement(SpecimenParameter specimenParameter, double displacement)
    {
        return specimenParameter.ConsiderLargeDisplacement
            ? throw new NotImplementedException($"The method '{nameof(CalculatePreloadedStrainFromDisplacement)}' was not implemented.")
            : (displacement - specimenParameter.PreLoadDisplacement) / specimenParameter.PreLoadLength;
    }

    /// <inheritdoc/>
    public double CalculatePreloadedStrainDerivativeFromDisplacement(SpecimenParameter specimenParameter, double displacement, double displacementDerivative)
    {
        throw new NotImplementedException($"The method '{nameof(CalculatePreloadedStrainDerivativeFromDisplacement)}' was not implemented.");
    }

    /// <inheritdoc/>
    public double CalculateForceFromStress(SpecimenParameter specimenParameter, double stress) => UnitConverter.ConvertMPaToPa(stress) * specimenParameter.Area;

    /// <inheritdoc/>
    public double CalculateStressFromForce(SpecimenParameter specimenParameter, double force) => UnitConverter.ConvertPaToMPa(force / specimenParameter.Area);

    /// <inheritdoc/>
    public double CalculateStressDerivativeFromForce(SpecimenParameter specimenParameter, double force, double forceDerivative)
    {
        throw new NotImplementedException($"The method '{nameof(CalculateStressDerivativeFromForce)}' was not implemented.");
    }
}
