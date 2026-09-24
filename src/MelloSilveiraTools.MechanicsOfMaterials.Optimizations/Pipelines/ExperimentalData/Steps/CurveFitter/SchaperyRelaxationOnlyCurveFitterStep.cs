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
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Schapery model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SchaperyRelaxationOnlyCurveFitterStep(
    ILogger<SchaperyRelaxationOnlyCurveFitterStep> logger,
    ISchaperyModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter,
    [FromKeyedServices(FunctionType.Logarithmic)] IMathematicalFunctionCurveFitter logarithmicCurveFitter,
    [FromKeyedServices(FunctionType.Exponential)] IMathematicalFunctionCurveFitter exponentialCurveFitter,
    FunctionFactory functionFactory)
    : MechanicalModelCurveFitterStepBase
{
    private static readonly double[] HelmholtzInitialParameters = [1.0, 1.0];
    private static readonly double[] HelmholtzLowerBounds = [0.0, 0.0];
    private static readonly double[] HelmholtzUpperBounds = [10.0, 10.0];
    private static readonly PolynomialFunction AnchorHelmholtzFunction = new([1.0]);

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.Schapery);

    /// <inheritdoc />
    protected override MechanicalBehaviorType MechanicalBehaviorType => MechanicalBehaviorType.StressStrain;

    /// <inheritdoc />
    protected override ViscoelasticEffect ViscoelasticEffect => ViscoelasticEffect.Relaxation;

    /// <inheritdoc />
    public override async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<CurveSegment> relaxations = [.. input.Where(cs => cs.Type == SegmentType.Relaxation).OrderBy(cs => cs.ExperimentalStrain[0])];
        if (relaxations.Count == 0)
        {
            yield break;
        }

        for (int i = 0; i < relaxations.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CurveSegment anchorSegment = relaxations[i];
            (MechanicalModelCurveFitOutput anchorOutput, double[] optimizedLinearParams) = FitAnchorSegment(anchorSegment);
            yield return anchorOutput;

            (double Strain, double He, double H2)[] strainAndHelmholtzVariables = new (double Strain, double He, double H2)[relaxations.Count - 1];

            double precision = anchorOutput.Precision;
            double totalError = anchorOutput.FinalError;
            int totalIterations = anchorOutput.Iterations;

            for (int j = 0; j < relaxations.Count; j++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (j == i)
                {
                    strainAndHelmholtzVariables[j] = (anchorSegment.ExperimentalStrain[0], 1.0, 1.0);
                    continue; // Skip the anchor segment
                }

                CurveSegment segment = relaxations[j];
                (MechanicalModelCurveFitOutput segmentFitResult, double he, double h2) = FitRemainingSegment(segment, optimizedLinearParams);

                strainAndHelmholtzVariables[j] = (segment.ExperimentalStrain[0], he, h2);

                // TODO: ESTUDAR MELHOR FORMA DE CALCULAR O ERRO FINAL.
                precision = (precision + segmentFitResult.Precision) / 2;
                totalError *= segmentFitResult.FinalError;
                totalIterations += segmentFitResult.Iterations;

                yield return segmentFitResult;
            }

            yield return BuildFinalOutput(relaxations, optimizedLinearParams, strainAndHelmholtzVariables, precision, totalError, totalIterations);
        }
    }

    private (MechanicalModelCurveFitOutput CurveFitOutput, double[] OptimizedLinearParams) FitAnchorSegment(CurveSegment anchorSegment)
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
        };
        CurveFitOutput anchorOutput = curveFitter.Fit(anchorInput);

        SchaperyConstitutiveParameters anchorParameters = new()
        {
            Ge = anchorOutput.OptimizedParameters[0],
            TransientRelaxationFunction = new PowerLaw([anchorOutput.OptimizedParameters[1], anchorOutput.OptimizedParameters[2]], initialStrain, finalStrain),
            He = new ConstantFunction(initialStrain, finalStrain, 1.0),
            H1 = new ConstantFunction(initialStrain, finalStrain, 1.0),
            H2 = new ConstantFunction(initialStrain, finalStrain, 1.0),
        };
        MechanicalModelCurveFitOutput output = CreateCurveFitOutput(anchorParameters, new AcceptedRange(initialStrain, finalStrain), anchorOutput.RSquared, anchorOutput.FinalError, anchorOutput.Iterations);
        return (output, anchorOutput.OptimizedParameters);
    }

    private (MechanicalModelCurveFitOutput Output, double He, double H2) FitRemainingSegment(CurveSegment segment, double[] optimizedLinearParams)
    {
        PowerLaw transientRelaxationFunction = new([optimizedLinearParams[1], optimizedLinearParams[2]]);
        CurveFitInput segmentInput = new()
        {
            IndependentVariables = [segment.TimePoints, segment.ExperimentalStrain],
            DependentVariable = segment.ExperimentalStress,
            Calculate = (parameters, xValues) =>
            {
                SchaperyConstitutiveParameters constitutiveParameters = new()
                {
                    Ge = optimizedLinearParams[0],
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
        };
        CurveFitOutput segmentOutput = curveFitter.Fit(segmentInput);

        double he = segmentOutput.OptimizedParameters[0];
        double h2 = segmentOutput.OptimizedParameters[1];

        double initialAcceptedStrain = segment.ExperimentalStrain[0];
        double finalAcceptedStrain = segment.ExperimentalStrain[^1];
        SchaperyConstitutiveParameters constitutiveParameters = new()
        {
            Ge = optimizedLinearParams[0],
            TransientRelaxationFunction = new PowerLaw([optimizedLinearParams[1], optimizedLinearParams[2]], initialAcceptedStrain, finalAcceptedStrain),
            He = new ConstantFunction(initialAcceptedStrain, finalAcceptedStrain, he),
            H1 = new ConstantFunction(initialAcceptedStrain, finalAcceptedStrain, 1.0),
            H2 = new ConstantFunction(initialAcceptedStrain, finalAcceptedStrain, h2),
        };
        AcceptedRange acceptedStrainRange = new(initialAcceptedStrain, finalAcceptedStrain);
        MechanicalModelCurveFitOutput output = CreateCurveFitOutput(constitutiveParameters, acceptedStrainRange, segmentOutput.RSquared, segmentOutput.FinalError, segmentOutput.Iterations);
        return (output, he, h2);
    }

    private MechanicalModelCurveFitOutput BuildFinalOutput(List<CurveSegment> relaxations, double[] optimizedLinearParams, (double Strain, double He, double H2)[] strainAndHelmholtzVariables, double precision, double totalError, int totalIterations)
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

        (Function Function, double Precision, double Error, int Iterations) heResult = FitHelmholtzVariable(strains, hePoints);
        (Function Function, double Precision, double Error, int Iterations) h2Result = FitHelmholtzVariable(strains, h2Points);

        // Incorporate error and iterations
        precision = (precision + heResult.Precision + h2Result.Precision) / 3;
        totalError *= (heResult.Error * h2Result.Error);
        totalIterations += (heResult.Iterations + h2Result.Iterations);

        double initialAcceptedStrain = relaxations[0].ExperimentalStrain[0];
        double finalAcceptedStrain = relaxations[^1].ExperimentalStrain[^1];
        SchaperyConstitutiveParameters constitutiveParameters = new()
        {
            Ge = optimizedLinearParams[0],
            TransientRelaxationFunction = new PowerLaw([optimizedLinearParams[1], optimizedLinearParams[2]], initialAcceptedStrain, finalAcceptedStrain),
            He = heResult.Function,
            H1 = new ConstantFunction(initialAcceptedStrain, finalAcceptedStrain, 1.0),
            H2 = h2Result.Function
        };
        return CreateCurveFitOutput(constitutiveParameters, new AcceptedRange(initialAcceptedStrain, finalAcceptedStrain), precision, totalError, totalIterations);
    }

    internal (Function Function, double Precision, double Error, int Iterations) FitHelmholtzVariable(double[] strain, double[] helmholtzVariable)
    {
        if (strain.Length == 1)
        {
            return (new ConstantFunction(strain[0], strain[0], helmholtzVariable[0]), 1, 0.0, 0);
        }

        // For logarithmic and exponential functions, we assume 2 parameters.
        const int numberOfParameters = 2;
        MathematicalCurveFitInput curveFitterInput = new()
        {
            NumberOfParameters = numberOfParameters,
            IndependentVariable = strain,
            DependentVariable = helmholtzVariable,
            Profile = CurveFitProfile.Automatic
        };

        List<(FunctionType Type, CurveFitOutput Output)> results = [];

        SafeResult<CurveFitInput, CurveFitOutput> logarithmicResult = logarithmicCurveFitter.TryFit(curveFitterInput with { ZeroBased = true });
        if (logarithmicResult.Success)
            results.Add((FunctionType.Logarithmic, logarithmicResult.Output!));
        else
            logger.LogWarning("It was not possible to fit the Helmholtz variable to a logarithmic function. Result: {@Result}", logarithmicResult);

        SafeResult<CurveFitInput, CurveFitOutput> exponentialResult = exponentialCurveFitter.TryFit(curveFitterInput);
        if (exponentialResult.Success)
            results.Add((FunctionType.Exponential, exponentialResult.Output!));
        else
            logger.LogWarning("It was not possible to fit the Helmholtz variable to a exponential function. Result: {@Result}", exponentialResult);

        if (results.Count == 0)
            throw new InvalidOperationException("All mathematical function curve fits failed.");

        (FunctionType bestType, CurveFitOutput bestOutput) = results.OrderBy(o => o.Output.FinalError).First();
        return (functionFactory.Create(bestType, strain[0], strain[^1], bestOutput.OptimizedParameters), bestOutput.RSquared, bestOutput.FinalError, bestOutput.Iterations);
    }
}
