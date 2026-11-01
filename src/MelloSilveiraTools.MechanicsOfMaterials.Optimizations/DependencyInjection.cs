using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations;

/// <summary>
/// Provides extension methods to register Optimizations services
/// (experimental data processing pipeline, parametric sweep pipeline, curve fitter steps, validators, and commands) into the DI container.
/// </summary>
public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers optimization services into the service collection.
        /// When <paramref name="settings"/> is provided, registers <see cref="ExperimentalDataSettings"/> as a singleton
        /// and wires up the experimental data processing pipeline.
        /// </summary>
        /// <param name="settings">
        /// Optional experimental data settings. When provided, registered as a singleton into DI.
        /// </param>
        /// <param name="curveFitterOptions">
        /// Optional curve fitter configuration options controlling deterministic seeding. When provided, registered as a singleton into DI.
        /// </param>
        /// <returns>The same service collection so additional registrations can be chained.</returns>
        public IServiceCollection AddOptimizationsServices(CurveFitterSettings? curveFitterOptions = null, ExperimentalDataSettings ? settings = null)
        {
            if (settings is not null)
            {
                services
                    .AddSingleton(settings)
                    .AddSingleton<ExecuteExperimentalDataPipeline>()
                    .AddSingleton<IValidator<ExecuteExperimentalDataPipelineRequest>, ProcessExperimentalDataValidator>()
                    .AddSingleton<IPipeline<ExperimentalDataProcessingInput, string>, ExperimentalDataProcessingPipeline>()
                    ;
            }

            return services
                .AddSingleton(curveFitterOptions ?? new CurveFitterSettings())
                // Register MathExpression curve fitter decorators.
                .AddKeyedSingleton<IMathExpressionCurveFitter, PronySeriesCurveFitter>(MathExpressionType.PronySeries)
                // Register MathematicalFunction curve fitter decorators.
                .AddKeyedSingleton<IFunctionCurveFitter, LogarithmicCurveFitter>(FunctionType.Logarithmic)
                .AddKeyedSingleton<IFunctionCurveFitter, ExponentialCurveFitter>(FunctionType.Exponential)
                .AddKeyedSingleton<IFunctionCurveFitter, PolynomialCurveFitter>(FunctionType.Polynomial)
                // Register Curve Fitter implementations.
                .AddKeyedSingleton<ICurveFitter, AlglibCurveFitter>(CurveFitterType.Alglib)
                .AddKeyedSingleton<ICurveFitter, MathNetCurveFitter>(CurveFitterType.MathNet)
                // Register the step factory.
                .AddSingleton<IMechanicalModelStepFactory, MechanicalModelStepFactory>()
                // Register Sensitivity Analysis services.
                .AddKeyedSingleton<IParametricSweepGenerator, FungParametricSweepGenerator>(nameof(MechanicalModel.Fung))
                .AddKeyedSingleton<IParametricSweepGenerator, SimplifiedFungParametricSweepGenerator>(nameof(MechanicalModel.SimplifiedFung))
                .AddKeyedSingleton<IParametricSweepGenerator, SchaperyParametricSweepGenerator>(nameof(MechanicalModel.Schapery))
                // Register pipelines.
                .AddSingleton<IPipeline<ParametricSweepInput, string>, ParametricSweepPipeline>()
                // Register Request Validators.
                .AddSingleton<IValidator<ExecuteParametricSweepPipelineRequest>, ExecuteParametricSweepValidator>()
                // Register Application Commands.
                .AddScoped<ExecuteParametricSweepPipeline>();
        }
    }
}
