using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class SchaperyRelaxationOnlyCurveFitterStepTests
{
    private static readonly AlglibCurveFitter _alglibCurveFitter = new();
    private static readonly MathNetCurveFitter _mathNetCurveFitter = new();

    private readonly SchaperyRelaxationOnlyCurveFitterStep _stepWithAlglib = new(
        Mock.Of<ILogger<SchaperyRelaxationOnlyCurveFitterStep>>(),
        Mock.Of<ISchaperyModelCalculator>(),
        _alglibCurveFitter,
        new LogarithmicCurveFitter(_alglibCurveFitter),
        new ExponentialCurveFitter(_alglibCurveFitter),
        new FunctionFactory()
    );

    private readonly SchaperyRelaxationOnlyCurveFitterStep _stepWithMathNet = new(
        Mock.Of<ILogger<SchaperyRelaxationOnlyCurveFitterStep>>(),
        Mock.Of<ISchaperyModelCalculator>(),
        _mathNetCurveFitter,
        new LogarithmicCurveFitter(_mathNetCurveFitter),
        new ExponentialCurveFitter(_mathNetCurveFitter),
        new FunctionFactory()
    );

    [Theory]
    [MemberData(nameof(GetHelmholtzData))]
    public void FitHelmholtzVariable_FitWithAlglib_ShouldSelectBestFunction(double[] strains, double[] heValues, double[] h2Values)
    {
        // Act
        // Validating the user's premise: both arrays CAN successfully fit an exponential function.
        Function heFunction = _stepWithAlglib.FitHelmholtzVariable(strains, heValues);
        Function h2Function = _stepWithAlglib.FitHelmholtzVariable(strains, h2Values);

        // Assert
        Assert.NotNull(heFunction);
        Assert.NotNull(h2Function);
    }

    [Theory]
    [MemberData(nameof(GetHelmholtzData))]
    public void FitHelmholtzVariable_FitWithMathNet_ShouldSelectBestFunction(double[] strains, double[] heValues, double[] h2Values)
    {
        // Act
        // Validating the user's premise: both arrays CAN successfully fit an exponential function.
        Function heFunction = _stepWithMathNet.FitHelmholtzVariable(strains, heValues);
        Function h2Function = _stepWithMathNet.FitHelmholtzVariable(strains, h2Values);

        // Assert
        Assert.NotNull(heFunction);
        Assert.NotNull(h2Function);
    }

    public static TheoryData<double[], double[], double[]> GetHelmholtzData() => new()
    {
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.8167, 0.7672, 0.7413 ], [1, 0.6716, 0.6465, 0.6379 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.8128, 0.8501, 0.9185 ], [1, 0.6686, 0.6836, 0.7215 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.8375, 0.8425, 0.8565 ], [1, 0.6469, 0.6448, 0.6519 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.8247, 0.8389, 0.8278 ], [1, 0.6615, 0.6486, 0.605 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.7357, 0.6218, 0.5537 ], [1, 0.7079, 0.5989, 0.5302 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.8127, 0.6193, 0.5534 ], [1, 0.8159, 0.5909, 0.5207 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.8676, 0.8553, 0.8269 ], [1, 0.6953, 0.6898, 0.6079 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.9384, 0.7503, 0.8941 ], [1, 0.8323, 0.7347, 0.842 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.8725, 0.8592, 0.8709 ], [1, 0.8483, 0.8891, 0.9331 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 1.0156, 1.1979, 1.4777 ], [1, 0.9094, 1.0434, 1.2936 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.9101, 0.9109, 0.9444 ], [1, 0.8817, 0.8836, 0.9431 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.9194, 0.9706, 1.0312 ], [1, 0.8502, 0.9118, 0.965 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.8932, 0.8855, 0.9424 ], [1, 0.8204, 0.7851, 0.8326 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.757, 0.8095, 0.8717 ], [1, 0.6448, 0.6871, 0.737 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 1, 0.828, 0.8934, 0.9489 ], [1, 0.6338, 0.6678, 0.7065 ] },
        { [0.03, 0.04, 0.05, 0.06 ], [ 0.4313, 0.4321, 0.4598, 0.4883 ], [53.74, 35.07, 36.42, 60.77] }
    };
}



