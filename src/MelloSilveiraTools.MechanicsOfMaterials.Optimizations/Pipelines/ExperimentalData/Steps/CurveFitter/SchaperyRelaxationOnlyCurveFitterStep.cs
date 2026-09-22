using MathNet.Numerics.Distributions;
using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
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
    : IMechanicalModelCurveFitterStep
{
    private readonly static double[] HelmholtzInitialParameters = [1.0, 1.0];
    private readonly static double[] HelmholtzLowerBounds = [0.0, 0.0];
    private readonly static double[] HelmholtzUpperBounds = [10.0, 10.0];

    /// <inheritdoc />
    public string Name => nameof(SchaperyRelaxationOnlyCurveFitterStep);

    /// <inheritdoc />
    public async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<CurveSegment> relaxations = [.. input.Where(cs => cs.Type == SegmentType.Relaxation).OrderBy(cs => cs.ExperimentalStrain[0])];
        if (relaxations.Count == 0)
            yield break;

        for (int i = 0; i < relaxations.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            CurveSegment anchorSegment = relaxations[i];
            var (anchorResult, optimizedLinearParams) = FitAnchorSegment(anchorSegment);
            yield return anchorResult;

            var strainAndHelmholtzVariables = new(double Strain, double He, double H2)[(relaxations.Count - 1)];

            double totalError = anchorResult.FinalError;
            int totalIterations = anchorResult.Iterations;

            for (int j = 0; j < relaxations.Count; j++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (j == i)
                {
                    strainAndHelmholtzVariables[j] = (anchorSegment.ExperimentalStrain[0], 1.0, 1.0);
                    continue; // Skip the anchor segment
                }

                CurveSegment segment = relaxations[j];
                var segmentFitResult = FitRemainingSegment(segment, optimizedLinearParams);

                strainAndHelmholtzVariables[j] = (segment.ExperimentalStrain[0], segmentFitResult.He, segmentFitResult.H2);

                // TODO: ESTUDAR MELHOR FORMA DE CALCULAR O ERRO FINAL.
                totalError *= segmentFitResult.FinalError;
                totalIterations += segmentFitResult.Iterations;

                yield return segmentFitResult.CurveFitOutput;
            }

            yield return BuildFinalOutput(relaxations, optimizedLinearParams, strainAndHelmholtzVariables, totalError, totalIterations);
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
                    TransientRelaxationFunction = new PowerLaw(null, null, [parameters[1], parameters[2]]),
                    He = new PolynomialFunction(null, null, [1.0]),
                    H1 = new PolynomialFunction(null, null, [1.0]),
                    H2 = new PolynomialFunction(null, null, [1.0]),
                };
                MechanicalModelInput<SchaperyConstitutiveParameters> currentInput = CreateModelInput(anchorSegment, xValues[1], constitutiveParameters);
                return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
            },
            InitialParameters = anchorInitialParams,
            LowerBounds = anchorLowerBounds,
            UpperBounds = anchorUpperBounds,
            EvaluateConstraintsAndPenalties = null,
        };
        CurveFitOutput anchorOutput = curveFitter.Fit(anchorInput);
        double[] optimizedLinearParams = anchorOutput.OptimizedParameters;

        SchaperyConstitutiveParameters anchorParameters = new()
        {
            Ge = optimizedLinearParams[0],
            TransientRelaxationFunction = new PowerLaw(initialStrain, finalStrain, [optimizedLinearParams[1], optimizedLinearParams[2]]),
            He = new ConstantFunction(initialStrain, finalStrain, 1.0),
            H1 = new ConstantFunction(initialStrain, finalStrain, 1.0),
            H2 = new ConstantFunction(initialStrain, finalStrain, 1.0),
        };
        MechanicalModelCurveFitOutput output = new(anchorParameters, anchorOutput.FinalError, anchorOutput.Iterations, new AcceptedRange(initialStrain, finalStrain));
        return (output, optimizedLinearParams);
    }

    private (MechanicalModelCurveFitOutput CurveFitOutput, double He, double H2, double FinalError, int Iterations) FitRemainingSegment(CurveSegment segment, double[] optimizedLinearParams)
    {
        CurveFitInput segmentInput = new()
        {
            IndependentVariables = [segment.TimePoints, segment.ExperimentalStrain],
            DependentVariable = segment.ExperimentalStress,
            Calculate = (parameters, xValues) =>
            {
                SchaperyConstitutiveParameters constitutiveParameters = new()
                {
                    Ge = optimizedLinearParams[0],
                    TransientRelaxationFunction = new PowerLaw(null, null, [optimizedLinearParams[1], optimizedLinearParams[2]]),
                    He = new PolynomialFunction(null, null, [parameters[0]]),
                    H1 = new PolynomialFunction(null, null, [1.0]),
                    H2 = new PolynomialFunction(null, null, [parameters[1]]),
                };
                MechanicalModelInput<SchaperyConstitutiveParameters> currentInput = CreateModelInput(segment, xValues[1], constitutiveParameters);
                return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
            },
            InitialParameters = HelmholtzInitialParameters,
            LowerBounds = HelmholtzLowerBounds,
            UpperBounds = HelmholtzUpperBounds,
            EvaluateConstraintsAndPenalties = null,
        };
        CurveFitOutput segmentOutput = curveFitter.Fit(segmentInput);

        double he = segmentOutput.OptimizedParameters[0];
        double h2 = segmentOutput.OptimizedParameters[1];

        double initialAcceptedStrain = segment.ExperimentalStrain[0];
        double finalAcceptedStrain = segment.ExperimentalStrain[^1];
        SchaperyConstitutiveParameters segmentParams = new()
        {
            Ge = optimizedLinearParams[0],
            TransientRelaxationFunction = new PowerLaw(initialAcceptedStrain, finalAcceptedStrain, [optimizedLinearParams[1], optimizedLinearParams[2]]),
            He = new ConstantFunction(initialAcceptedStrain, finalAcceptedStrain, he),
            H1 = new ConstantFunction(initialAcceptedStrain, finalAcceptedStrain, 1.0),
            H2 = new ConstantFunction(initialAcceptedStrain, finalAcceptedStrain, h2),
        };
        MechanicalModelCurveFitOutput output = new(segmentParams, segmentOutput.FinalError, segmentOutput.Iterations, new AcceptedRange(initialAcceptedStrain, finalAcceptedStrain));

        return (output, he, h2, segmentOutput.FinalError, segmentOutput.Iterations);
    }

    private static MechanicalModelInput<SchaperyConstitutiveParameters> CreateModelInput(CurveSegment segment, double strain, SchaperyConstitutiveParameters constitutiveParameters) => new()
    {
        MechanicalModelName = nameof(MechanicalModel.Schapery),
        AcceptedStrainRange = new AcceptedRange(segment.ExperimentalStrain[0], segment.ExperimentalStrain[^1]),
        MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
        RampTimeConsideration = RampTimeConsideration.Disregard,
        ViscoelasticEffect = ViscoelasticEffect.Relaxation,
        Strain = new MechanicalParameter(strain),
        Stress = new MechanicalParameter(segment.ExperimentalStress[0]),
        TimeStep = segment.TimePoints[1] - segment.TimePoints[0],
        ConstitutiveParameters = constitutiveParameters
    };

    private MechanicalModelCurveFitOutput BuildFinalOutput(List<CurveSegment> relaxations, double[] optimizedLinearParams, (double Strain, double He, double H2)[] strainAndHelmholtzVariables, double totalError, int totalIterations)
    {
        var strains = new double[strainAndHelmholtzVariables.Length];
        var hePoints = new double[strainAndHelmholtzVariables.Length];
        var h2Points = new double[strainAndHelmholtzVariables.Length];

        for (int i = 0; i < strainAndHelmholtzVariables.Length; i++)
        {
            strains[i] = strainAndHelmholtzVariables[i].Strain;
            hePoints[i] = strainAndHelmholtzVariables[i].He;
            h2Points[i] = strainAndHelmholtzVariables[i].H2;
        }

        Function heFunction = FitHelmholtzVariable(strains, hePoints);
        Function h2Function = FitHelmholtzVariable(strains, h2Points);

        double initialAcceptedStrain = relaxations[0].ExperimentalStrain[0];
        double finalAcceptedStrain = relaxations[^1].ExperimentalStrain[^1];
        SchaperyConstitutiveParameters finalParams = new()
        {
            Ge = optimizedLinearParams[0],
            TransientRelaxationFunction = new PowerLaw(initialAcceptedStrain, finalAcceptedStrain, [optimizedLinearParams[1], optimizedLinearParams[2]]),
            He = heFunction,
            H1 = new ConstantFunction(initialAcceptedStrain, finalAcceptedStrain, 1.0),
            H2 = h2Function
        };
        return new MechanicalModelCurveFitOutput(finalParams, totalError, totalIterations, new AcceptedRange(initialAcceptedStrain, finalAcceptedStrain));
    }

    internal Function FitHelmholtzVariable(double[] strain, double[] helmholtzVariable)
    {
        if (strain.Length == 1)
            return new ConstantFunction(strain[0], strain[0], helmholtzVariable[0]);

        // For logarithmic and exponential functions, we assume 2 parameters.
        const int numberOfParameters = 2;

        List<(FunctionType Type, CurveFitOutput Output)> results = [];

        var logarithmicResult = logarithmicCurveFitter.TryFit(numberOfParameters, strain, helmholtzVariable, zeroBased: true);
        if (logarithmicResult.Success)
            results.Add((FunctionType.Logarithmic, logarithmicResult.Output!));
        else
            logger.LogWarning("It was not possible to fit the Helmholtz variable to a logarithmic function. Result: {@Result}", logarithmicResult);

        var exponentialResult = exponentialCurveFitter.TryFit(numberOfParameters, strain, helmholtzVariable);
        if (exponentialResult.Success)
            results.Add((FunctionType.Exponential, exponentialResult.Output!));
        else
            logger.LogWarning("It was not possible to fit the Helmholtz variable to a exponential function. Result: {@Result}", exponentialResult);

        if (results.Count == 0)
            throw new InvalidOperationException("All mathematical function curve fits failed.");

        (FunctionType bestType, CurveFitOutput bestOutput) = results.OrderBy(o => o.Output.FinalError).First();
        return functionFactory.Create(bestType, strain[0], strain[^1], bestOutput.OptimizedParameters);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
