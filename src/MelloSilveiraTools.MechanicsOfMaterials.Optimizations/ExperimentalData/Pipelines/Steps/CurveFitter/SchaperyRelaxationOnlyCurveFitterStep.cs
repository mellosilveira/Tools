using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Schapery model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
/// <param name="logger">The logger instance.</param>
/// <param name="mechanicalModelCalculator">The calculator for Schapery viscoelastic constitutive equations.</param>
/// <param name="curveFitter">The optimization engine for non-linear regression.</param>
/// <param name="logarithmicCurveFitter">Logarithmic curve fitter for Helmholtz variables.</param>
/// <param name="exponentialCurveFitter">Exponential curve fitter for Helmholtz variables.</param>
/// <param name="polynomialCurveFitter">Polynomial curve fitter fallback for Helmholtz variables.</param>
/// <param name="functionFactory">Factory constructing mathematical functions.</param>
/// <param name="targetRSquared">Mandatory target R² determination coefficient for early-exit and precision monitoring.</param>
/// <param name="confidenceLevel">Desired confidence level for parameter interval estimation (e.g. 0.95 for 95%). Defaults to 0.95.</param>
public sealed class SchaperyRelaxationOnlyCurveFitterStep(
    ILogger<SchaperyRelaxationOnlyCurveFitterStep> logger,
    ISchaperyModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter,
    [FromKeyedServices(FunctionType.Logarithmic)] IFunctionCurveFitter logarithmicCurveFitter,
    [FromKeyedServices(FunctionType.Exponential)] IFunctionCurveFitter exponentialCurveFitter,
    [FromKeyedServices(FunctionType.Polynomial)] IFunctionCurveFitter polynomialCurveFitter,
    FunctionFactory functionFactory,
    double targetRSquared,
    double confidenceLevel = 0.95)
    : MechanicalModelCurveFitterStepBase(targetRSquared, confidenceLevel)
{
    private static readonly double[] HelmholtzInitialParameters = [1.0, 1.0];
    private static readonly double[] HelmholtzLowerBounds = [0.0, 0.0];
    private static readonly double[] HelmholtzUpperBounds = [10.0, 10.0];
    private static readonly PolynomialFunction AnchorHelmholtzFunction = new([1.0]);

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.Schapery);

    /// <inheritdoc />
    protected override LoadResponseRelationship LoadResponseRelationship => LoadResponseRelationship.StressStrain;

    /// <inheritdoc />
    protected override ViscoelasticEffect ViscoelasticEffect => ViscoelasticEffect.Relaxation;

    private readonly List<CurveSegment> _relaxations = new();

    /// <inheritdoc />
    public override async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment segment, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (segment.Type == SegmentType.Relaxation)
        {
            _relaxations.Add(segment);
        }
        else if (segment.Type == SegmentType.Unknown)
        {
            List<CurveSegment> relaxations = [.. _relaxations.OrderBy(cs => cs.ExperimentalStrain[0])];
            if (relaxations.Count == 0)
            {
                yield break;
            }

            for (int i = 0; i < relaxations.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                CurveSegment anchorSegment = relaxations[i];
                (MechanicalModelCurveFitOutput anchorOutput, IReadOnlyList<MechanicalModelParameterConfidenceInterval> linearParameters) = FitAnchorSegment(anchorSegment);
                yield return anchorOutput;

                (double Strain, double He, double H2)[] strainAndHelmholtzVariables = new (double Strain, double He, double H2)[relaxations.Count];

                double weightedRSquaredSum = anchorOutput.RSquared * anchorSegment.ExperimentalStrain.Length;
                double weightSum = anchorSegment.ExperimentalStrain.Length;
                double errorSum = anchorOutput.FinalError;
                int totalIterations = anchorOutput.Iterations;

                for (int j = 0; j < relaxations.Count; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (j == i)
                    {
                        strainAndHelmholtzVariables[j] = (anchorSegment.ExperimentalStrain[0], 1.0, 1.0);
                        continue; // Skip the anchor segment
                    }

                    CurveSegment currentSegment = relaxations[j];
                    (MechanicalModelCurveFitOutput remainingOutput, double he, double h2) = FitRemainingSegment(currentSegment, linearParameters);
                    yield return remainingOutput;

                    strainAndHelmholtzVariables[j] = (currentSegment.ExperimentalStrain[0], he, h2);

                    weightedRSquaredSum += remainingOutput.RSquared * currentSegment.ExperimentalStrain.Length;
                    weightSum += currentSegment.ExperimentalStrain.Length;
                    errorSum += remainingOutput.FinalError;
                    totalIterations += remainingOutput.Iterations;
                }

                cancellationToken.ThrowIfCancellationRequested();

                double precision = weightedRSquaredSum / weightSum;
                double totalError = errorSum / relaxations.Count;

                MechanicalModelCurveFitOutput finalOutput = BuildFinalOutput(relaxations, linearParameters, strainAndHelmholtzVariables, precision, totalError, totalIterations);
                yield return finalOutput;
            }

            _relaxations.Clear();
        }

        await Task.CompletedTask;
    }

    private (MechanicalModelCurveFitOutput CurveFitOutput, IReadOnlyList<MechanicalModelParameterConfidenceInterval> LinearParameters) FitAnchorSegment(CurveSegment anchorSegment)
    {
        double initialStrain = anchorSegment.ExperimentalStrain[0];
        double finalStrain = anchorSegment.ExperimentalStrain[^1];
        double finalStress = anchorSegment.ExperimentalStress[^1];

        double initialGe = Math.Round(finalStress / finalStrain, 2);
        double[] anchorInitialParams = [initialGe, 0.1, 0.05];
        double[] anchorLowerBounds = [1e-6, 0.0, 0.0];
        double[] anchorUpperBounds = [1e5, 1e3, 10.0];

        CurveFitInput anchorInput = new()
        {
            IndependentVariables = [anchorSegment.TimePoints, anchorSegment.ExperimentalStrain],
            DependentVariable = anchorSegment.ExperimentalStress,
            Calculate = (parameters, xValues) =>
            {
                SchaperyConstitutiveParameters constitutiveParameters = new()
                {
                    Ge = parameters[0],
                    TransientRelaxationFunction = new PowerLaw([parameters[1], parameters[2]]),
                    He = AnchorHelmholtzFunction,
                    H1 = AnchorHelmholtzFunction,
                    H2 = AnchorHelmholtzFunction,
                };
                MechanicalModelInput<SchaperyConstitutiveParameters> currentInput = CreateModelInput(anchorSegment, constitutiveParameters);
                return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
            },
            InitialParameters = anchorInitialParams,
            LowerBounds = anchorLowerBounds,
            UpperBounds = anchorUpperBounds,
            Profile = CurveFitProfile.Automatic,
            TargetRSquared = TargetRSquared,
            ConfidenceLevel = ConfidenceLevel
        };
        CurveFitOutput anchorOutput = curveFitter.Fit(anchorInput);

        SchaperyConstitutiveParameters anchorParameters = new()
        {
            Ge = anchorOutput.Parameters[0].Value,
            TransientRelaxationFunction = new PowerLaw([anchorOutput.Parameters[1].Value, anchorOutput.Parameters[2].Value], initialStrain, finalStrain),
            He = new ConstantFunction(1.0, initialStrain, finalStrain),
            H1 = new ConstantFunction(1.0, initialStrain, finalStrain),
            H2 = new ConstantFunction(1.0, initialStrain, finalStrain),
        };

        List<MechanicalModelParameterConfidenceInterval> labeledLinearParameters =
        [
            new(0, anchorOutput.Parameters[0].Value, anchorOutput.Parameters[0].LowerBound, anchorOutput.Parameters[0].UpperBound, anchorOutput.Parameters[0].StandardError, anchorOutput.Parameters[0].ConfidenceLevel, nameof(SchaperyConstitutiveParameters.Ge)),
            new(1, anchorOutput.Parameters[1].Value, anchorOutput.Parameters[1].LowerBound, anchorOutput.Parameters[1].UpperBound, anchorOutput.Parameters[1].StandardError, anchorOutput.Parameters[1].ConfidenceLevel, $"{nameof(SchaperyConstitutiveParameters.TransientRelaxationFunction)}_A"),
            new(2, anchorOutput.Parameters[2].Value, anchorOutput.Parameters[2].LowerBound, anchorOutput.Parameters[2].UpperBound, anchorOutput.Parameters[2].StandardError, anchorOutput.Parameters[2].ConfidenceLevel, $"{nameof(SchaperyConstitutiveParameters.TransientRelaxationFunction)}_n")
        ];

        MechanicalModelCurveFitOutput output = CreateCurveFitOutput([anchorSegment], anchorParameters, new AcceptedRange(initialStrain, finalStrain), anchorOutput.RSquared, anchorOutput.FinalError, anchorOutput.Iterations, labeledLinearParameters);
        return (output, labeledLinearParameters);
    }

    private (MechanicalModelCurveFitOutput Output, double He, double H2) FitRemainingSegment(CurveSegment segment, IReadOnlyList<MechanicalModelParameterConfidenceInterval> linearParameters)
    {
        PowerLaw transientRelaxationFunction = new([linearParameters[1].Value, linearParameters[2].Value]);
        CurveFitInput segmentInput = new()
        {
            IndependentVariables = [segment.TimePoints, segment.ExperimentalStrain],
            DependentVariable = segment.ExperimentalStress,
            Calculate = (parameters, xValues) =>
            {
                SchaperyConstitutiveParameters constitutiveParameters = new()
                {
                    Ge = linearParameters[0].Value,
                    TransientRelaxationFunction = transientRelaxationFunction,
                    He = new ConstantFunction(parameters[0]),
                    H1 = AnchorHelmholtzFunction,
                    H2 = new ConstantFunction(parameters[1]),
                };
                MechanicalModelInput<SchaperyConstitutiveParameters> currentInput = CreateModelInput(segment, constitutiveParameters);
                return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
            },
            InitialParameters = HelmholtzInitialParameters,
            LowerBounds = HelmholtzLowerBounds,
            UpperBounds = HelmholtzUpperBounds,
            Profile = CurveFitProfile.Automatic,
            TargetRSquared = TargetRSquared,
            ConfidenceLevel = ConfidenceLevel
        };
        CurveFitOutput segmentOutput = curveFitter.Fit(segmentInput);

        double he = segmentOutput.Parameters[0].Value;
        double h2 = segmentOutput.Parameters[1].Value;

        double initialAcceptedStrain = segment.ExperimentalStrain[0];
        double finalAcceptedStrain = segment.ExperimentalStrain[^1];
        SchaperyConstitutiveParameters constitutiveParameters = new()
        {
            Ge = linearParameters[0].Value,
            TransientRelaxationFunction = new PowerLaw([linearParameters[1].Value, linearParameters[2].Value], initialAcceptedStrain, finalAcceptedStrain),
            He = new ConstantFunction(he, initialAcceptedStrain, finalAcceptedStrain),
            H1 = new ConstantFunction(1.0, initialAcceptedStrain, finalAcceptedStrain),
            H2 = new ConstantFunction(h2, initialAcceptedStrain, finalAcceptedStrain),
        };
        AcceptedRange acceptedStrainRange = new(initialAcceptedStrain, finalAcceptedStrain);

        List<MechanicalModelParameterConfidenceInterval> labeledSegmentParameters =
        [
            new(0, segmentOutput.Parameters[0].Value, segmentOutput.Parameters[0].LowerBound, segmentOutput.Parameters[0].UpperBound, segmentOutput.Parameters[0].StandardError, segmentOutput.Parameters[0].ConfidenceLevel, nameof(SchaperyConstitutiveParameters.He)),
            new(1, segmentOutput.Parameters[1].Value, segmentOutput.Parameters[1].LowerBound, segmentOutput.Parameters[1].UpperBound, segmentOutput.Parameters[1].StandardError, segmentOutput.Parameters[1].ConfidenceLevel, nameof(SchaperyConstitutiveParameters.H2))
        ];

        MechanicalModelCurveFitOutput output = CreateCurveFitOutput([segment], constitutiveParameters, acceptedStrainRange, segmentOutput.RSquared, segmentOutput.FinalError, segmentOutput.Iterations, labeledSegmentParameters);
        return (output, he, h2);
    }

    private MechanicalModelCurveFitOutput BuildFinalOutput(
        List<CurveSegment> relaxations,
        IReadOnlyList<MechanicalModelParameterConfidenceInterval> linearParameters,
        (double Strain, double He, double H2)[] strainAndHelmholtzVariables,
        double precision,
        double totalError,
        int totalIterations)
    {
        double[] strains = new double[strainAndHelmholtzVariables.Length];
        double[] hePoints = new double[strainAndHelmholtzVariables.Length];
        double[] h2Points = new double[strainAndHelmholtzVariables.Length];

        for (int i = 0; i < strainAndHelmholtzVariables.Length; i++)
        {
            strains[i] = strainAndHelmholtzVariables[i].Strain;
            hePoints[i] = strainAndHelmholtzVariables[i].He;
            h2Points[i] = strainAndHelmholtzVariables[i].H2;
        }

        (Function Function, double Precision, double Error, int Iterations, IReadOnlyList<ParameterConfidenceInterval> Parameters) heResult = FitHelmholtzVariable(strains, hePoints);
        (Function Function, double Precision, double Error, int Iterations, IReadOnlyList<ParameterConfidenceInterval> Parameters) h2Result = FitHelmholtzVariable(strains, h2Points);

        // Incorporate error and iterations
        precision = (precision + heResult.Precision + h2Result.Precision) / 3.0;
        totalError *= (heResult.Error * h2Result.Error);
        totalIterations += (heResult.Iterations + h2Result.Iterations);

        double initialAcceptedStrain = relaxations[0].ExperimentalStrain[0];
        double finalAcceptedStrain = relaxations[^1].ExperimentalStrain[^1];
        SchaperyConstitutiveParameters constitutiveParameters = new()
        {
            Ge = linearParameters[0].Value,
            TransientRelaxationFunction = new PowerLaw([linearParameters[1].Value, linearParameters[2].Value], initialAcceptedStrain, finalAcceptedStrain),
            He = heResult.Function,
            H1 = new ConstantFunction(1.0, initialAcceptedStrain, finalAcceptedStrain),
            H2 = h2Result.Function
        };

        List<MechanicalModelParameterConfidenceInterval> combinedParameters = [];
        int index = 0;
        for (int k = 0; k < linearParameters.Count; k++)
        {
            MechanicalModelParameterConfidenceInterval p = linearParameters[k];
            combinedParameters.Add(p with { Index = index++ });
        }

        for (int k = 0; k < heResult.Parameters.Count; k++)
        {
            ParameterConfidenceInterval p = heResult.Parameters[k];
            combinedParameters.Add(new MechanicalModelParameterConfidenceInterval(index++, p.Value, p.LowerBound, p.UpperBound, p.StandardError, p.ConfidenceLevel, $"{nameof(SchaperyConstitutiveParameters.He)}_{k}"));
        }

        for (int k = 0; k < h2Result.Parameters.Count; k++)
        {
            ParameterConfidenceInterval p = h2Result.Parameters[k];
            combinedParameters.Add(new MechanicalModelParameterConfidenceInterval(index++, p.Value, p.LowerBound, p.UpperBound, p.StandardError, p.ConfidenceLevel, $"{nameof(SchaperyConstitutiveParameters.H2)}_{k}"));
        }

        return CreateCurveFitOutput(relaxations, constitutiveParameters, new AcceptedRange(initialAcceptedStrain, finalAcceptedStrain), precision, totalError, totalIterations, combinedParameters);
    }

    private (Function Function, double Precision, double Error, int Iterations, IReadOnlyList<ParameterConfidenceInterval> Parameters) FitHelmholtzVariable(double[] strain, double[] helmholtzVariable)
    {
        if (strain.Length == 1)
        {
            ParameterConfidenceInterval constantParam = new(0, helmholtzVariable[0], helmholtzVariable[0], helmholtzVariable[0], 0.0, ConfidenceLevel);
            return (new ConstantFunction(helmholtzVariable[0], strain[0], strain[0]), 1.0, 0.0, 0, [constantParam]);
        }

        // For logarithmic and exponential functions, we assume 2 parameters.
        const int numberOfParameters = 2;
        MathematicalCurveFitInput baseInput = new()
        {
            NumberOfParameters = numberOfParameters,
            IndependentVariable = strain,
            DependentVariable = helmholtzVariable,
            Profile = CurveFitProfile.Automatic,
            TargetRSquared = TargetRSquared,
            ConfidenceLevel = ConfidenceLevel
        };

        List<(FunctionType Type, CurveFitOutput Output)> candidates = [];

        // 1. Primary candidates (Logarithmic and Exponential) requiring user-specified target R² threshold
        TryAddCandidate(candidates, FunctionType.Logarithmic, logarithmicCurveFitter.TryFit(baseInput with { ZeroBased = true }), TargetRSquared);
        TryAddCandidate(candidates, FunctionType.Exponential, exponentialCurveFitter.TryFit(baseInput), TargetRSquared);

        // 2. Fallback to polynomial fitting if primary candidates failed or did not meet R² threshold
        if (candidates.Count == 0)
        {
            for (int parameterCount = 2; parameterCount <= strain.Length; parameterCount++)
            {
                SafeResult<CurveFitInput, CurveFitOutput> polynomialResult = polynomialCurveFitter.TryFit(baseInput with { NumberOfParameters = parameterCount });
                if (polynomialResult.Success)
                    candidates.Add((FunctionType.Polynomial, polynomialResult.Output!));
                else
                    logger.LogWarning("It was not possible to fit the Helmholtz variable to a polynomial function with {Count} parameters. Result: {@Result}", parameterCount, polynomialResult);
            }
        }

        if (candidates.Count == 0)
            throw new InvalidOperationException("All mathematical function curve fits failed.");

        (FunctionType bestType, CurveFitOutput bestOutput) = candidates.OrderBy(o => o.Output.FinalError).First();
        return (functionFactory.Create(bestType, strain[0], strain[^1], bestOutput.OptimizedParameters), bestOutput.RSquared, bestOutput.FinalError, bestOutput.Iterations, bestOutput.Parameters);
    }

    private void TryAddCandidate(List<(FunctionType Type, CurveFitOutput Output)> candidates, FunctionType functionType, SafeResult<CurveFitInput, CurveFitOutput> fitResult, double? minimumRSquared = null)
    {
        if (fitResult.Success && (minimumRSquared is null || fitResult.Output!.RSquared >= minimumRSquared.Value))
        {
            candidates.Add((functionType, fitResult.Output!));
            return;
        }

        if (!fitResult.Success)
            logger.LogWarning("It was not possible to fit the Helmholtz variable to a {Type} function. Result: {@Result}", functionType, fitResult);
        else
            logger.LogWarning("Helmholtz candidate {Type} fit succeeded (R² = {RSquared:F4}) but did not meet minimum threshold ({MinR2:F4}).", functionType, fitResult.Output!.RSquared, minimumRSquared);
    }
}
