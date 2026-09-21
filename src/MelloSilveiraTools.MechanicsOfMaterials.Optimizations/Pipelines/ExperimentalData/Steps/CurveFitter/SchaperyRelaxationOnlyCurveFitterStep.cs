using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Schapery model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SchaperyRelaxationOnlyCurveFitterStep(
    ISchaperyModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public string Name => nameof(SchaperyRelaxationOnlyCurveFitterStep);

    /// <inheritdoc />
    public async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        List<CurveSegment> relaxations = [.. input.Where(cs => cs.Type == SegmentType.Relaxation).OrderBy(cs => cs.ExperimentalStrain[0])];
        if (relaxations.Count == 0)
            yield break;

        CurveSegment anchorSegment = relaxations[0];
        var (anchorResult, optimizedLinearParams) = FitAnchorSegment(anchorSegment);
        yield return anchorResult;

        List<double> strains = [anchorSegment.ExperimentalStrain[0]];
        List<double> hePoints = [1.0];
        List<double> h2Points = [1.0];
        double totalError = anchorResult.FinalError;
        int totalIterations = anchorResult.Iterations;

        for (int i = 1; i < relaxations.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            CurveSegment segment = relaxations[i];
            var segmentFitResult = FitRemainingSegment(segment, optimizedLinearParams);

            strains.Add(segment.ExperimentalStrain[0]);
            hePoints.Add(segmentFitResult.He);
            h2Points.Add(segmentFitResult.H2);
            
            totalError += segmentFitResult.FinalError;
            totalIterations += segmentFitResult.Iterations;

            yield return segmentFitResult.CurveFitOutput;
        }

        yield return BuildFinalOutput(relaxations, optimizedLinearParams, strains, hePoints, h2Points, totalError, totalIterations);
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
        double[] segmentInitialParams = [1.0, 1.0];
        double[] segmentLowerBounds = [0.0, 0.0];
        double[] segmentUpperBounds = [10.0, 10.0];

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
            InitialParameters = segmentInitialParams,
            LowerBounds = segmentLowerBounds,
            UpperBounds = segmentUpperBounds,
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

    private MechanicalModelCurveFitOutput BuildFinalOutput(List<CurveSegment> relaxations, double[] optimizedLinearParams, List<double> strains, List<double> hePoints, List<double> h2Points, double totalError, int totalIterations)
    {
        Function heFunction = FindBestMathematicalFunction([.. strains], [.. hePoints]);
        Function h2Function = FindBestMathematicalFunction([.. strains], [.. h2Points]);

        double initialAcceptedStrain = relaxations[0].ExperimentalStrain[0];
        double finalAcceptedStrain = relaxations[^1].ExperimentalStrain[0];
        
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

    private Function FindBestMathematicalFunction(double[] x, double[] y)
    {
        if (x.Length == 1)
            return new ConstantFunction(x[0], x[0], y[0]);

        int n = x.Length;
        double minX = x.Min();
        double maxX = x.Max();

        var results = new List<(FunctionType Type, CurveFitOutput Output)>();

        // 1. Polynomial
        try
        {
            int polyParamCount = n;
            double[] polyLower = Enumerable.Repeat(-1e6, polyParamCount).ToArray();
            double[] polyUpper = Enumerable.Repeat(1e6, polyParamCount).ToArray();
            double[] polyInitial = new double[polyParamCount];
            polyInitial[0] = 1.0;
            CurveFitInput polyInput = new()
            {
                IndependentVariables = [x],
                DependentVariable = y,
                Calculate = (p, xValues) => new PolynomialFunction(minX, maxX, p).Calculate(xValues[0]),
                LowerBounds = polyLower,
                UpperBounds = polyUpper,
                InitialParameters = polyInitial,
            };
            results.Add((FunctionType.Polynomial, curveFitter.Fit(polyInput)));
        }
        catch { }

        // 2. Logarithmic
        try
        {
            int logParamCount = 2 * n + 1;
            double[] logLower = Enumerable.Repeat(-1e6, logParamCount).ToArray();
            double[] logUpper = Enumerable.Repeat(1e6, logParamCount).ToArray();
            double[] logInitial = new double[logParamCount];
            logInitial[0] = 1.0; 
            for (int i = 0; i < n; i++) 
            {
                logInitial[2 * i + 1] = 1.0; 
                logInitial[2 * i + 2] = 1.0; 
            }
            CurveFitInput logInput = new()
            {
                IndependentVariables = [x],
                DependentVariable = y,
                Calculate = (p, xValues) => new LogarithmicFunction(minX, maxX, p).Calculate(xValues[0]),
                LowerBounds = logLower,
                UpperBounds = logUpper,
                InitialParameters = logInitial,
            };
            results.Add((FunctionType.Logarithmic, curveFitter.Fit(logInput)));
        }
        catch { }

        // 3. Exponential
        try
        {
            int expParamCount = 2 * n;
            double[] expLower = Enumerable.Repeat(-1e6, expParamCount).ToArray();
            double[] expUpper = Enumerable.Repeat(1e6, expParamCount).ToArray();
            double[] expInitial = new double[expParamCount];
            for (int i = 0; i < n; i++) expInitial[2 * i] = 1.0; 
            CurveFitInput expInput = new()
            {
                IndependentVariables = [x],
                DependentVariable = y,
                Calculate = (p, xValues) => new ExponencialFunction(minX, maxX, p).Calculate(xValues[0]),
                LowerBounds = expLower,
                UpperBounds = expUpper,
                InitialParameters = expInitial,
            };
            results.Add((FunctionType.Exponential, curveFitter.Fit(expInput)));
        }
        catch { }

        if (results.Count == 0)
            throw new InvalidOperationException("All mathematical function curve fits failed.");

        var best = results.OrderBy(o => o.Output.FinalError).First();
        return best.Type switch
        {
            FunctionType.Polynomial => new PolynomialFunction(minX, maxX, best.Output.OptimizedParameters),
            FunctionType.Exponential => new ExponencialFunction(minX, maxX, best.Output.OptimizedParameters),
            _ => new LogarithmicFunction(minX, maxX, best.Output.OptimizedParameters),
        };
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

