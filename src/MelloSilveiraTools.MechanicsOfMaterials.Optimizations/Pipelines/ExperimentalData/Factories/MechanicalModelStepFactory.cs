namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;

using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter.Fung;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of <see cref="IMechanicalModelStepFactory"/> that instantiates
/// concrete <see cref="IMechanicalModelCurveFitterStep"/> instances and decorators directly using
/// the <c>new()</c> directive, without relying on <see cref="IServiceProvider"/>.
/// </summary>
/// <param name="serviceLocator">The service locator for resolving keyed services.</param>
/// <param name="loggerFactory">The logger factory for creating step-specific loggers.</param>
public sealed class MechanicalModelStepFactory(ServiceLocator serviceLocator, ILoggerFactory loggerFactory) : IMechanicalModelStepFactory
{
    /// <inheritdoc />
    public IMechanicalModelCurveFitterStep Create(string mechanicalModelName, IReadOnlyList<SegmentType> targetSegments, CurveFitterType curveFitterType, double targetRSquared)
    {
        ICurveFitter curveFitter = serviceLocator.GetRequiredKeyedService<ICurveFitter>(curveFitterType);

        return (mechanicalModelName, targetSegments) switch
        {
            (nameof(MechanicalModel.Fung), [SegmentType.Relaxation]) => new FungRelaxationOnlyCurveFitterStep(
                serviceLocator.GetRequiredService<IFungModelCalculator>(),
                curveFitter,
                targetRSquared),

            (nameof(MechanicalModel.SimplifiedFung), [SegmentType.Relaxation]) => new SimplifiedFungRelaxationOnlyCurveFitterStep(
                serviceLocator.GetRequiredService<ISimplifiedFungModelCalculator>(),
                curveFitter,
                new PronySeriesCurveFitter(curveFitter),
                targetRSquared),

            (nameof(MechanicalModel.Schapery), [SegmentType.Relaxation]) => new SchaperyRelaxationOnlyCurveFitterStep(
                loggerFactory.CreateLogger<SchaperyRelaxationOnlyCurveFitterStep>(),
                serviceLocator.GetRequiredService<ISchaperyModelCalculator>(),
                curveFitter,
                serviceLocator.GetRequiredKeyedService<IFunctionCurveFitter>(FunctionType.Logarithmic),
                serviceLocator.GetRequiredKeyedService<IFunctionCurveFitter>(FunctionType.Exponential),
                serviceLocator.GetRequiredKeyedService<IFunctionCurveFitter>(FunctionType.Polynomial),
                serviceLocator.GetRequiredService<FunctionFactory>(),
                targetRSquared),

            _ => throw new ArgumentException($"Unknown mechanical model or unsupported segment configuration: '{mechanicalModelName}'.", nameof(mechanicalModelName))
        };
    }
}
