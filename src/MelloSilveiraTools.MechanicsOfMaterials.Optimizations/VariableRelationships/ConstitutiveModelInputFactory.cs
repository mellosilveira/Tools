using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships;

/// <summary>
/// Factory utility responsible for dynamically instantiating strongly-typed <see cref="ConstitutiveParameters"/>
/// and packaging them into a <see cref="GenericMechanicalModelInput"/> envelope for numerical simulation.
/// </summary>
public static class ConstitutiveModelInputFactory
{
    private const double DefaultStrain = 0.05;
    private const double DefaultTimeStep = 0.01;

    /// <summary>
    /// Creates a configured <see cref="GenericMechanicalModelInput"/> populated with the model-specific
    /// constitutive parameters extracted from the provided parameter map.
    /// </summary>
    /// <param name="mechanicalModelName">The registered name of the mechanical model.</param>
    /// <param name="parameters">The dictionary of parameter names and evaluated values.</param>
    /// <returns>A new <see cref="GenericMechanicalModelInput"/> instance ready for stress evaluation.</returns>
    public static GenericMechanicalModelInput CreateInput(string mechanicalModelName, IReadOnlyDictionary<string, double> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mechanicalModelName);
        ArgumentNullException.ThrowIfNull(parameters);

        ConstitutiveParameters constitutiveParameters = CreateConstitutiveParameters(mechanicalModelName, parameters);
        double strainValue = GetParameterValue(parameters, DefaultStrain, "Strain", "strain");

        MechanicalModelInput baseInput = new()
        {
            MechanicalModelName = mechanicalModelName,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            TimeStep = DefaultTimeStep,
            AcceptedStrainRange = AcceptedRange.Default,
            Strain = new MechanicalParameter(strainValue),
            LoadResponseRelationship = LoadResponseRelationship.StressStrain,
            ViscoelasticEffect = ViscoelasticEffect.Relaxation
        };

        return new GenericMechanicalModelInput(baseInput, constitutiveParameters);
    }

    private static ConstitutiveParameters CreateConstitutiveParameters(string mechanicalModelName, IReadOnlyDictionary<string, double> parameters)
    {
        if (string.Equals(mechanicalModelName, nameof(MechanicalModel.Fung), StringComparison.OrdinalIgnoreCase))
        {
            double elasticStress = GetParameterValue(parameters, 1000.0, nameof(FungConstitutiveParameters.ElasticStressConstant), "A", "C1", "c1");
            double elasticPower = GetParameterValue(parameters, 1.0, nameof(FungConstitutiveParameters.ElasticPowerConstant), "B", "C2", "c2");
            double relaxationStiffness = GetParameterValue(parameters, 0.1, nameof(ReducedRelaxationFunction.RelaxationStiffness), "ReducedRelaxationFunction.RelaxationStiffness", "C", "c");
            double fastRelaxation = GetParameterValue(parameters, 0.01, nameof(ReducedRelaxationFunction.FastRelaxationTime), "ReducedRelaxationFunction.FastRelaxationTime", "tau1", "Tau1");
            double slowRelaxation = GetParameterValue(parameters, 10.0, nameof(ReducedRelaxationFunction.SlowRelaxationTime), "ReducedRelaxationFunction.SlowRelaxationTime", "tau2", "Tau2");

            ReducedRelaxationFunction reducedFunction = new(relaxationStiffness, fastRelaxation, slowRelaxation);
            return new FungConstitutiveParameters(elasticStress, elasticPower, reducedFunction);
        }

        if (string.Equals(mechanicalModelName, nameof(MechanicalModel.SimplifiedFung), StringComparison.OrdinalIgnoreCase))
        {
            double elasticStress = GetParameterValue(parameters, 1000.0, nameof(SimplifiedFungConstitutiveParameters.ElasticStressConstant), "A", "C1", "c1");
            double elasticPower = GetParameterValue(parameters, 1.0, nameof(SimplifiedFungConstitutiveParameters.ElasticPowerConstant), "B", "C2", "c2");
            PronySeries pronySeries = new(0.1, [1.0]);
            return new SimplifiedFungConstitutiveParameters(elasticStress, elasticPower, pronySeries);
        }

        if (string.Equals(mechanicalModelName, nameof(MechanicalModel.Maxwell), StringComparison.OrdinalIgnoreCase))
        {
            double stiffness = GetParameterValue(parameters, 1000.0, nameof(MaxwellConstitutiveParameters.Stiffness), "E", "ElasticStressConstant");
            double viscosity = GetParameterValue(parameters, 100.0, nameof(MaxwellConstitutiveParameters.Viscosity), "eta", "Viscosity");
            return new MaxwellConstitutiveParameters { Stiffness = stiffness, Viscosity = viscosity };
        }

        if (string.Equals(mechanicalModelName, nameof(MechanicalModel.Elastic), StringComparison.OrdinalIgnoreCase))
        {
            double youngModulus = GetParameterValue(parameters, 1000.0, nameof(ElasticConstitutiveParameters.YoungModulus), "E", "ElasticStressConstant", "Stiffness");
            return new ElasticConstitutiveParameters { YoungModulus = youngModulus };
        }

        if (string.Equals(mechanicalModelName, nameof(MechanicalModel.Schapery), StringComparison.OrdinalIgnoreCase))
        {
            double g0 = GetParameterValue(parameters, 1.0, nameof(SchaperyConstitutiveParameters.G0), "G0");
            double g1 = GetParameterValue(parameters, 1.0, nameof(SchaperyConstitutiveParameters.G1), "G1");
            double g2 = GetParameterValue(parameters, 1.0, nameof(SchaperyConstitutiveParameters.G2), "G2");
            double j0 = GetParameterValue(parameters, 1.0, nameof(SchaperyConstitutiveParameters.J0), "J0");
            double ge = GetParameterValue(parameters, 1.0, nameof(SchaperyConstitutiveParameters.Ge), "Ge");
            return new SchaperyConstitutiveParameters { G0 = g0, G1 = g1, G2 = g2, J0 = j0, Ge = ge };
        }

        // Generic fallback to ElasticConstitutiveParameters with mapped modulus
        double fallbackModulus = GetParameterValue(parameters, 1000.0, "YoungModulus", "E", "ElasticStressConstant");
        return new ElasticConstitutiveParameters { YoungModulus = fallbackModulus };
    }

    private static double GetParameterValue(IReadOnlyDictionary<string, double> parameters, double defaultValue, params string[] aliases)
    {
        for (int i = 0; i < aliases.Length; i++)
        {
            string alias = aliases[i];
            if (parameters.TryGetValue(alias, out double value))
            {
                return value;
            }

            foreach (KeyValuePair<string, double> kvp in parameters)
            {
                if (string.Equals(kvp.Key, alias, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
        }

        return defaultValue;
    }
}
