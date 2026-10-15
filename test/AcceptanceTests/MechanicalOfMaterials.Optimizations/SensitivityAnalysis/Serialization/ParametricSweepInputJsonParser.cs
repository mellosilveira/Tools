using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using System.Text.Json;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.SensitivityAnalysis.Serialization;

/// <summary>
/// Deserializes JSON representations into strongly-typed <see cref="ParametricSweepInput"/> instances,
/// including polymorphic resolution of constitutive model parameters and mathematical functions.
/// </summary>
public static class ParametricSweepInputJsonParser
{
    private static readonly JsonSerializerOptions DefaultOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Parses a JSON text into a <see cref="ParametricSweepInput"/> instance.
    /// </summary>
    /// <param name="json">The JSON payload string.</param>
    /// <returns>A populated <see cref="ParametricSweepInput"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON payload is malformed or missing required properties.</exception>
    public static ParametricSweepInput Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        string? identifier = GetStringProperty(root, "identifier");
        string mechanicalModelName = GetStringProperty(root, "mechanicalModelName")
            ?? throw new JsonException("Property 'mechanicalModelName' is required in ParametricSweepInput JSON.");

        double baseStrain = GetDoubleProperty(root, "baseStrain", 0.0);

        RangeParameters? strainRange = null;
        if (TryGetPropertyIgnoreCase(root, "strainRange", out JsonElement srElem) && srElem.ValueKind == JsonValueKind.Object)
        {
            strainRange = srElem.Deserialize<RangeParameters>(DefaultOptions);
        }

        RampTimeConsideration rampTimeConsideration = RampTimeConsideration.Disregard;
        if (TryGetPropertyIgnoreCase(root, "rampTimeConsideration", out JsonElement rtcElem))
        {
            if (rtcElem.ValueKind == JsonValueKind.String)
            {
                rampTimeConsideration = Enum.Parse<RampTimeConsideration>(rtcElem.GetString()!, true);
            }
            else if (rtcElem.ValueKind == JsonValueKind.Number)
            {
                rampTimeConsideration = (RampTimeConsideration)rtcElem.GetInt32();
            }
        }

        double? rampTime = TryGetPropertyIgnoreCase(root, "rampTime", out JsonElement rtElem) && rtElem.ValueKind == JsonValueKind.Number
            ? rtElem.GetDouble()
            : null;

        double initialTime = GetDoubleProperty(root, "initialTime", 0.0);
        double finalTime = GetDoubleProperty(root, "finalTime", 10.0);
        double timeStep = GetDoubleProperty(root, "timeStep", 0.1);

        double asymptoteTolerance = 1e-3;
        if (TryGetPropertyIgnoreCase(root, "asymptoteTolerance", out JsonElement atElem) && atElem.ValueKind == JsonValueKind.Number)
        {
            asymptoteTolerance = atElem.GetDouble();
        }
        else if (TryGetPropertyIgnoreCase(root, "tolerance", out JsonElement tolElem) && tolElem.ValueKind == JsonValueKind.Number)
        {
            asymptoteTolerance = tolElem.GetDouble();
        }

        string outputDirectory = GetStringProperty(root, "outputDirectory") ?? string.Empty;

        Dictionary<string, RangeParameters> constantVariables = [];
        if (TryGetPropertyIgnoreCase(root, "constantVariables", out JsonElement cvElem) && cvElem.ValueKind == JsonValueKind.Object)
        {
            constantVariables = cvElem.Deserialize<Dictionary<string, RangeParameters>>(DefaultOptions) ?? [];
        }

        Dictionary<string, Dictionary<int, RangeParameters>> arrayVariables = [];
        if (TryGetPropertyIgnoreCase(root, "arrayVariables", out JsonElement avElem) && avElem.ValueKind == JsonValueKind.Object)
        {
            arrayVariables = avElem.Deserialize<Dictionary<string, Dictionary<int, RangeParameters>>>(DefaultOptions) ?? [];
        }

        if (!TryGetPropertyIgnoreCase(root, "baseline", out JsonElement baselineElem) || baselineElem.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Property 'baseline' is required and must be a JSON object.");
        }

        ConstitutiveParameters baseline = ParseBaseline(mechanicalModelName, baselineElem);

        return new ParametricSweepInput
        {
            Identifier = identifier,
            MechanicalModelName = mechanicalModelName,
            Baseline = baseline,
            ConstantVariables = constantVariables,
            ArrayVariables = arrayVariables,
            BaseStrain = baseStrain,
            StrainRange = strainRange,
            RampTimeConsideration = rampTimeConsideration,
            RampTime = rampTime,
            InitialTime = initialTime,
            FinalTime = finalTime,
            TimeStep = timeStep,
            AsymptoteTolerance = asymptoteTolerance,
            OutputDirectory = outputDirectory
        };
    }

    private static ConstitutiveParameters ParseBaseline(string modelName, JsonElement baselineElement)
    {
        FunctionFactory functionFactory = new();

        if (modelName.Equals(nameof(MechanicalModel.Fung), StringComparison.OrdinalIgnoreCase))
        {
            double elasticStressConstant = GetDoubleProperty(baselineElement, "elasticStressConstant", GetDoubleProperty(baselineElement, "A", 0.0));
            double elasticPowerConstant = GetDoubleProperty(baselineElement, "elasticPowerConstant", GetDoubleProperty(baselineElement, "B", 0.0));

            ReducedRelaxationFunction reducedRelaxation;
            if (TryGetPropertyIgnoreCase(baselineElement, "reducedRelaxationFunction", out JsonElement redElement))
            {
                double c = GetDoubleProperty(redElement, "relaxationStiffness", GetDoubleProperty(redElement, "c", 0.0));
                double tau1 = GetDoubleProperty(redElement, "fastRelaxationTime", GetDoubleProperty(redElement, "tau1", 0.0));
                double tau2 = GetDoubleProperty(redElement, "slowRelaxationTime", GetDoubleProperty(redElement, "tau2", 0.0));
                reducedRelaxation = new ReducedRelaxationFunction(c, tau1, tau2);
            }
            else
            {
                double c = GetDoubleProperty(baselineElement, "c", 0.0);
                double tau1 = GetDoubleProperty(baselineElement, "tau1", 0.0);
                double tau2 = GetDoubleProperty(baselineElement, "tau2", 0.0);
                reducedRelaxation = new ReducedRelaxationFunction(c, tau1, tau2);
            }

            return new FungConstitutiveParameters(elasticStressConstant, elasticPowerConstant, reducedRelaxation);
        }

        if (modelName.Equals(nameof(MechanicalModel.SimplifiedFung), StringComparison.OrdinalIgnoreCase))
        {
            double elasticStressConstant = GetDoubleProperty(baselineElement, "elasticStressConstant", GetDoubleProperty(baselineElement, "A", 0.0));
            double elasticPowerConstant = GetDoubleProperty(baselineElement, "elasticPowerConstant", GetDoubleProperty(baselineElement, "B", 0.0));

            PronySeries pronySeries;
            if (TryGetPropertyIgnoreCase(baselineElement, "reducedRelaxationFunction", out JsonElement pronyElement))
            {
                double independentParam = GetDoubleProperty(pronyElement, "independentParameter", GetDoubleProperty(pronyElement, "c", 0.0));
                double[] iteratorCoefficients = [];
                if (TryGetPropertyIgnoreCase(pronyElement, "iteratorCoefficients", out JsonElement iterElem) && iterElem.ValueKind == JsonValueKind.Array)
                {
                    iteratorCoefficients = iterElem.Deserialize<double[]>(DefaultOptions) ?? [];
                }
                pronySeries = new PronySeries(independentParam, iteratorCoefficients);
            }
            else
            {
                pronySeries = new PronySeries(0.0, []);
            }

            return new SimplifiedFungConstitutiveParameters(elasticStressConstant, elasticPowerConstant, pronySeries);
        }

        if (modelName.Equals(nameof(MechanicalModel.Schapery), StringComparison.OrdinalIgnoreCase))
        {
            double ge = GetDoubleProperty(baselineElement, "ge", 0.0);
            double g0 = GetDoubleProperty(baselineElement, "g0", 1.0);
            double g1 = GetDoubleProperty(baselineElement, "g1", 1.0);
            double g2 = GetDoubleProperty(baselineElement, "g2", 1.0);
            double j0 = GetDoubleProperty(baselineElement, "j0", 0.0);

            Function? he = TryGetPropertyIgnoreCase(baselineElement, "he", out JsonElement heElem) ? ParseFunction(heElem, functionFactory) : null;
            Function? h1 = TryGetPropertyIgnoreCase(baselineElement, "h1", out JsonElement h1Elem) ? ParseFunction(h1Elem, functionFactory) : null;
            Function? h2 = TryGetPropertyIgnoreCase(baselineElement, "h2", out JsonElement h2Elem) ? ParseFunction(h2Elem, functionFactory) : null;

            PowerLaw? transientRelaxation = null;
            if (TryGetPropertyIgnoreCase(baselineElement, "transientRelaxationFunction", out JsonElement trElem) && trElem.ValueKind == JsonValueKind.Object)
            {
                double[] coefficients = [];
                if (TryGetPropertyIgnoreCase(trElem, "coefficients", out JsonElement coefElem) && coefElem.ValueKind == JsonValueKind.Array)
                {
                    coefficients = coefElem.Deserialize<double[]>(DefaultOptions) ?? [];
                }
                transientRelaxation = new PowerLaw(coefficients);
            }

            PronySeries? transientCreep = null;
            if (TryGetPropertyIgnoreCase(baselineElement, "transientCreepCompliance", out JsonElement tcElem) && tcElem.ValueKind == JsonValueKind.Object)
            {
                double c = GetDoubleProperty(tcElem, "independentParameter", 0.0);
                double[] iter = [];
                if (TryGetPropertyIgnoreCase(tcElem, "iteratorCoefficients", out JsonElement iterElem) && iterElem.ValueKind == JsonValueKind.Array)
                {
                    iter = iterElem.Deserialize<double[]>(DefaultOptions) ?? [];
                }
                transientCreep = new PronySeries(c, iter);
            }

            return new SchaperyConstitutiveParameters
            {
                Ge = ge,
                G0 = g0,
                G1 = g1,
                G2 = g2,
                J0 = j0,
                He = he,
                H1 = h1,
                H2 = h2,
                TransientRelaxationFunction = transientRelaxation,
                TransientCreepCompliance = transientCreep
            };
        }

        if (modelName.Equals(nameof(MechanicalModel.ModifiedSuperpositionMethod), StringComparison.OrdinalIgnoreCase))
        {
            Function? initialYoungModulus = TryGetPropertyIgnoreCase(baselineElement, "initialYoungModulus", out JsonElement iymElem)
                ? ParseFunction(iymElem, functionFactory)
                : null;

            PolynomialFunction? stressRelaxationRate = null;
            if (TryGetPropertyIgnoreCase(baselineElement, "stressRelaxationRate", out JsonElement srrElem) && srrElem.ValueKind == JsonValueKind.Object)
            {
                double[] coefficients = [];
                if (TryGetPropertyIgnoreCase(srrElem, "coefficients", out JsonElement coefElem) && coefElem.ValueKind == JsonValueKind.Array)
                {
                    coefficients = coefElem.Deserialize<double[]>(DefaultOptions) ?? [];
                }
                stressRelaxationRate = new PolynomialFunction(coefficients);
            }

            return new ModifiedSuperpositionMethodConstitutiveParameters
            {
                InitialYoungModulus = initialYoungModulus,
                StressRelaxationRate = stressRelaxationRate
            };
        }

        if (modelName.Equals(nameof(MechanicalModel.Maxwell), StringComparison.OrdinalIgnoreCase))
        {
            double viscosity = GetDoubleProperty(baselineElement, "viscosity", 0.0);
            double stiffness = GetDoubleProperty(baselineElement, "stiffness", 0.0);
            return new MaxwellConstitutiveParameters { Viscosity = viscosity, Stiffness = stiffness };
        }

        if (modelName.Equals(nameof(MechanicalModel.Elastic), StringComparison.OrdinalIgnoreCase))
        {
            double youngModulus = GetDoubleProperty(baselineElement, "youngModulus", 0.0);
            return new ElasticConstitutiveParameters { YoungModulus = youngModulus };
        }

        throw new NotSupportedException($"Mechanical model '{modelName}' is not supported for JSON baseline deserialization.");
    }

    private static Function ParseFunction(JsonElement element, FunctionFactory functionFactory)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            return new ConstantFunction(element.GetDouble());
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            string typeStr = GetStringProperty(element, "type") ?? nameof(FunctionType.Polynomial);
            FunctionType functionType = Enum.Parse<FunctionType>(typeStr, true);
            double[] coefficients = [];
            if (TryGetPropertyIgnoreCase(element, "coefficients", out JsonElement coefElem) && coefElem.ValueKind == JsonValueKind.Array)
            {
                coefficients = coefElem.Deserialize<double[]>(DefaultOptions) ?? [];
            }

            double? initialVal = TryGetPropertyIgnoreCase(element, "initialVariableValue", out JsonElement iv) && iv.ValueKind == JsonValueKind.Number
                ? iv.GetDouble()
                : null;

            double? finalVal = TryGetPropertyIgnoreCase(element, "finalVariableValue", out JsonElement fv) && fv.ValueKind == JsonValueKind.Number
                ? fv.GetDouble()
                : null;

            return functionFactory.Create(functionType, initialVal, finalVal, coefficients);
        }

        throw new JsonException($"Cannot parse Function from JSON element kind {element.ValueKind}.");
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (JsonProperty prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static double GetDoubleProperty(JsonElement element, string propertyName, double defaultValue)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out JsonElement prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetDouble();
        }
        return defaultValue;
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out JsonElement prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }
}
