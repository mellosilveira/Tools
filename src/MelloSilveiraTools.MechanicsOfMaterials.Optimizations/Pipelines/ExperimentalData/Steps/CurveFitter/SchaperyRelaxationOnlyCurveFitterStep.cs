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
        var relaxations = input.Where(cs => cs.Type == SegmentType.Relaxation).OrderBy(cs => cs.ExperimentalStrain[0]).ToList();
        if (relaxations.Count == 0)
            yield break;

        var anchorSegment = relaxations[0];
        
        // 1. Fit Anchor (Optimize Ge, C, n ONLY) -> array size 3
        double[] anchorInitialParams = [1000.0, 0.1, 0.05];
        double[] anchorLowerBounds = [1e-6, 0.0, 0.0];
        double[] anchorUpperBounds = [1e5, 1e3, 10.0];

        CurveFitInput anchorInput = new()
        {
            IndependentVariables = [anchorSegment.TimePoints, anchorSegment.ExperimentalStrain],
            DependentVariable = anchorSegment.ExperimentalStress,
            Calculate = (parameters, xValues) =>
            {
                MechanicalModelInput<SchaperyConstitutiveParameters> currentInput = new()
                {
                    MechanicalModelName = nameof(MechanicalModel.Schapery),
                    AcceptedStrainRange = new AcceptedRange { InitialPoint = anchorSegment.ExperimentalStrain[0], FinalPoint = anchorSegment.ExperimentalStrain[^1] },
                    MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                    RampTimeConsideration = RampTimeConsideration.Disregard,
                    ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                    Strain = new MechanicalParameter(xValues[1]),
                    Stress = new MechanicalParameter(anchorSegment.ExperimentalStress[0]),
                    TimeStep = anchorSegment.TimePoints[1] - anchorSegment.TimePoints[0],
                    ConstitutiveParameters = new SchaperyConstitutiveParameters
                    {
                        Ge = parameters[0],
                        TransientRelaxationFunction = new PowerLaw(null, null, [parameters[1], parameters[2]]),
                        He = new PolynomialFunction(null, null, [1.0]),
                        H1 = new PolynomialFunction(null, null, [1.0]),
                        H2 = new PolynomialFunction(null, null, [1.0]),
                    },
                };
                return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
            },
            LowerBounds = anchorLowerBounds,
            UpperBounds = anchorUpperBounds,
            EvaluateConstraintsAndPenalties = null,
            InitialParameters = anchorInitialParams,
        };

        CurveFitOutput anchorOutput = curveFitter.Fit(anchorInput);
        double[] optimizedLinearParams = anchorOutput.OptimizedParameters;

        SchaperyConstitutiveParameters anchorParams = new SchaperyConstitutiveParameters
        {
            Ge = optimizedLinearParams[0],
            TransientRelaxationFunction = new PowerLaw(anchorSegment.ExperimentalStrain[0], anchorSegment.ExperimentalStrain[^1], [optimizedLinearParams[1], optimizedLinearParams[2]]),
            He = new ConstantFunction(anchorSegment.ExperimentalStrain[0], anchorSegment.ExperimentalStrain[^1], 1.0),
            H1 = new ConstantFunction(anchorSegment.ExperimentalStrain[0], anchorSegment.ExperimentalStrain[^1], 1.0),
            H2 = new ConstantFunction(anchorSegment.ExperimentalStrain[0], anchorSegment.ExperimentalStrain[^1], 1.0),
        };
        yield return new MechanicalModelCurveFitOutput(anchorParams, anchorOutput.FinalError, anchorOutput.Iterations, new AcceptedRange { InitialPoint = anchorSegment.ExperimentalStrain[0], FinalPoint = anchorSegment.ExperimentalStrain[^1] });

        List<double> strains = new() { anchorSegment.ExperimentalStrain[0] };
        List<double> hePoints = new() { 1.0 };
        List<double> h2Points = new() { 1.0 };
        double totalError = anchorOutput.FinalError;
        int totalIterations = anchorOutput.Iterations;

        for (int i = 1; i < relaxations.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var segment = relaxations[i];

            // 2. Fit Remaining Relaxations (Optimize he, h2 ONLY) -> array size 2
            double[] segmentInitialParams = [1.0, 1.0];
            double[] segmentLowerBounds = [0.0, 0.0];
            double[] segmentUpperBounds = [10.0, 10.0];

            CurveFitInput segmentInput = new()
            {
                IndependentVariables = [segment.TimePoints, segment.ExperimentalStrain],
                DependentVariable = segment.ExperimentalStress,
                Calculate = (parameters, xValues) =>
                {
                    MechanicalModelInput<SchaperyConstitutiveParameters> currentInput = new()
                    {
                        MechanicalModelName = nameof(MechanicalModel.Schapery),
                        AcceptedStrainRange = new AcceptedRange { InitialPoint = segment.ExperimentalStrain[0], FinalPoint = segment.ExperimentalStrain[^1] },
                        MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                        RampTimeConsideration = RampTimeConsideration.Disregard,
                        ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                        Strain = new MechanicalParameter(xValues[1]),
                        Stress = new MechanicalParameter(segment.ExperimentalStress[0]),
                        TimeStep = segment.TimePoints[1] - segment.TimePoints[0],
                        ConstitutiveParameters = new SchaperyConstitutiveParameters
                        {
                            Ge = optimizedLinearParams[0],
                            TransientRelaxationFunction = new PowerLaw(null, null, [optimizedLinearParams[1], optimizedLinearParams[2]]),
                            He = new PolynomialFunction(null, null, [parameters[0]]),
                            H1 = new PolynomialFunction(null, null, [1.0]),
                            H2 = new PolynomialFunction(null, null, [parameters[1]]),
                        },
                    };
                    return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
                },
                LowerBounds = segmentLowerBounds,
                UpperBounds = segmentUpperBounds,
                EvaluateConstraintsAndPenalties = null,
                InitialParameters = segmentInitialParams,
            };

            CurveFitOutput segmentOutput = curveFitter.Fit(segmentInput);
            strains.Add(segment.ExperimentalStrain[0]);
            hePoints.Add(segmentOutput.OptimizedParameters[0]); // he is now index 0
            h2Points.Add(segmentOutput.OptimizedParameters[1]); // h2 is now index 1
            
            totalError += segmentOutput.FinalError;
            totalIterations += segmentOutput.Iterations;

            double initialAcceptedStrain = segment.ExperimentalStrain[0];
            double finalAcceptedStrain = segment.ExperimentalStrain[^1];
            SchaperyConstitutiveParameters segmentParams = new()
            {
                Ge = optimizedLinearParams[0],
                TransientRelaxationFunction = new PowerLaw(initialAcceptedStrain, finalAcceptedStrain, [optimizedLinearParams[1], optimizedLinearParams[2]]),
                He = new ConstantFunction(segment.ExperimentalStrain[0], segment.ExperimentalStrain[^1], segmentOutput.OptimizedParameters[0]),
                H1 = new ConstantFunction(segment.ExperimentalStrain[0], segment.ExperimentalStrain[^1], 1.0),
                H2 = new ConstantFunction(segment.ExperimentalStrain[0], segment.ExperimentalStrain[^1], segmentOutput.OptimizedParameters[1]),
            };
            yield return new MechanicalModelCurveFitOutput(segmentParams, segmentOutput.FinalError, segmentOutput.Iterations, new AcceptedRange(segment.ExperimentalStrain[0], segment.ExperimentalStrain[^1]));
        }

        Function heFunction = FindBestMathematicalFunction(strains.ToArray(), hePoints.ToArray(), curveFitter);
        Function h2Function = FindBestMathematicalFunction(strains.ToArray(), h2Points.ToArray(), curveFitter);

        double initialStrain = relaxations[0].ExperimentalStrain[0];
        double finalStrain = relaxations[^1].ExperimentalStrain[0];

        SchaperyConstitutiveParameters finalParams = new SchaperyConstitutiveParameters
        {
            Ge = optimizedLinearParams[0],
            TransientRelaxationFunction = new PowerLaw(initialStrain, finalStrain, [optimizedLinearParams[1], optimizedLinearParams[2]]),
            He = heFunction,
            H1 = new ConstantFunction(initialStrain, finalStrain, 1.0),
            H2 = h2Function
        };

        yield return new MechanicalModelCurveFitOutput(finalParams, totalError, totalIterations, new AcceptedRange { InitialPoint = initialStrain, FinalPoint = finalStrain });
    }

    private Function FindBestMathematicalFunction(double[] x, double[] y, ICurveFitter curveFitter)
    {
        if (x.Length == 1)
            return new ConstantFunction(x[0], x[0], y[0]);

        int n = x.Length;

        // 1. Polynomial: n terms -> n coefficients
        int polyParamCount = n;
        double[] polyLower = Enumerable.Repeat(-1e6, polyParamCount).ToArray();
        double[] polyUpper = Enumerable.Repeat(1e6, polyParamCount).ToArray();
        double[] polyInitial = new double[polyParamCount];
        polyInitial[0] = 1.0;
        CurveFitInput polyInput = new()
        {
            IndependentVariables = [x],
            DependentVariable = y,
            Calculate = (p, xValues) =>
            {
                double e = xValues[0];
                double val = p[^1];
                for (int j = p.Length - 2; j >= 0; j--) val = val * e + p[j];
                return val;
            },
            LowerBounds = polyLower,
            UpperBounds = polyUpper,
            InitialParameters = polyInitial,
        };

        // 2. Logarithmic: n terms -> 2n + 1 coefficients (a0, and pairs of a1, a2...)
        int logParamCount = 2 * n + 1;
        double[] logLower = Enumerable.Repeat(-1e6, logParamCount).ToArray();
        double[] logUpper = Enumerable.Repeat(1e6, logParamCount).ToArray();
        double[] logInitial = new double[logParamCount];
        logInitial[0] = 1.0; // a0
        for (int i = 0; i < n; i++) 
        {
            logInitial[2 * i + 1] = 1.0; // a_odd
            logInitial[2 * i + 2] = 1.0; // a_even
        }
        CurveFitInput logInput = new()
        {
            IndependentVariables = [x],
            DependentVariable = y,
            Calculate = (p, xValues) =>
            {
                double e = xValues[0];
                double val = p[0];
                int k = (p.Length - 1) / 2;
                for (int i = 0; i < k; i++)
                {
                    double a_odd = p[2 * i + 1];
                    double a_even = p[2 * i + 2];
                    val += a_odd * Math.Log(Math.Max(a_even * e, 1e-12));
                }
                return val;
            },
            LowerBounds = logLower,
            UpperBounds = logUpper,
            InitialParameters = logInitial,
        };

        // 3. Exponential: n terms -> 2n coefficients
        int expParamCount = 2 * n;
        double[] expLower = Enumerable.Repeat(-1e6, expParamCount).ToArray();
        double[] expUpper = Enumerable.Repeat(1e6, expParamCount).ToArray();
        double[] expInitial = new double[expParamCount];
        for (int i = 0; i < n; i++) expInitial[2 * i] = 1.0; // set a0, a2... to 1.0
        CurveFitInput expInput = new()
        {
            IndependentVariables = [x],
            DependentVariable = y,
            Calculate = (p, xValues) =>
            {
                double e = xValues[0];
                double val = 0;
                for (int j = 0; j < p.Length / 2; j++) val += p[2 * j] * Math.Exp(p[2 * j + 1] * e);
                return val;
            },
            LowerBounds = expLower,
            UpperBounds = expUpper,
            InitialParameters = expInitial,
        };

        CurveFitOutput polyOutput = curveFitter.Fit(polyInput);
        CurveFitOutput logOutput = curveFitter.Fit(logInput);
        CurveFitOutput expOutput = curveFitter.Fit(expInput);

        var best = new[]
        {
            new { Type = FunctionType.Polynomial, Output = polyOutput },
            new { Type = FunctionType.Logarithmic, Output = logOutput },
            new { Type = FunctionType.Exponential, Output = expOutput }
        }.OrderBy(o => o.Output.FinalError).First();

        double minX = x.Min();
        double maxX = x.Max();

        if (best.Type == FunctionType.Polynomial)
            return new PolynomialFunction(minX, maxX, best.Output.OptimizedParameters);
        if (best.Type == FunctionType.Exponential)
            return new ExponencialFunction(minX, maxX, best.Output.OptimizedParameters);

        return new LogarithmicFunction(minX, maxX, best.Output.OptimizedParameters);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
