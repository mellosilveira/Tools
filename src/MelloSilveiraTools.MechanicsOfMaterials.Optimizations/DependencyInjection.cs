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
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Single;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Steps.Generators;
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
        /// Mechanical models are registered automatically as required dependencies.
        /// When <paramref name="experimentalDataSettings"/> is provided, registers <see cref="ExperimentalDataSettings"/> as a singleton
        /// and wires up the experimental data processing pipeline.
        /// When <paramref name="parametricSweepSettings"/> is provided, registers <see cref="ParametricSweepSettings"/> as a singleton
        /// and wires up the parametric sweep pipeline.
        /// </summary>
        /// <param name="curveFitterSettings">
        /// Optional curve fitter configuration options controlling deterministic seeding and default fitter engine. When omitted, default settings are used.
        /// </param>
        /// <param name="experimentalDataSettings">
        /// Optional experimental data settings. When provided, registered as a singleton into DI.
        /// </param>
        /// <param name="parametricSweepSettings">
        /// Optional parametric sweep settings. When provided, registered as a singleton into DI.
        /// </param>
        /// <returns>The same service collection so additional registrations can be chained.</returns>
        public IServiceCollection AddOptimizationsServices(
            CurveFitterSettings? curveFitterSettings = null,
            ExperimentalDataSettings? experimentalDataSettings = null,
            ParametricSweepSettings? parametricSweepSettings = null)
        {
            if (curveFitterSettings is not null)
            {
                services
                    .AddSingleton(curveFitterSettings)
                    .AddSingleton<IMechanicalModelStepFactory, MechanicalModelStepFactory>()
                    .AddSingleton(provider => provider.GetRequiredKeyedService<ICurveFitter>(curveFitterSettings.DefaultCurveFitter))
                    // Register Curve Fitter implementations.
                    .AddKeyedSingleton<ICurveFitter, AlglibCurveFitter>(CurveFitterType.Alglib)
                    .AddKeyedSingleton<ICurveFitter, MathNetCurveFitter>(CurveFitterType.MathNet)
                    // Register MathExpression curve fitter decorators.
                    .AddKeyedSingleton<IMathExpressionCurveFitter, PronySeriesCurveFitter>(MathExpressionType.PronySeries)
                    // Register MathematicalFunction curve fitter decorators.
                    .AddKeyedSingleton<IFunctionCurveFitter, LogarithmicCurveFitter>(FunctionType.Logarithmic)
                    .AddKeyedSingleton<IFunctionCurveFitter, ExponentialCurveFitter>(FunctionType.Exponential)
                    .AddKeyedSingleton<IFunctionCurveFitter, PolynomialCurveFitter>(FunctionType.Polynomial);
            }

            if (experimentalDataSettings is not null)
            {
                services
                    .AddSingleton(experimentalDataSettings)
                    .AddSingleton<ExecuteExperimentalDataPipeline>()
                    .AddSingleton<ExecuteBatchExperimentalDataPipeline>()
                    .AddSingleton<IValidator<ExecuteExperimentalDataPipelineRequest>, ExecuteExperimentalDataPipelineValidator>()
                    .AddSingleton<IValidator<ExecuteBatchExperimentalDataPipelineRequest>, ExecuteBatchExperimentalDataPipelineValidator>()
                    .AddSingleton<IPipeline<ExperimentalDataProcessingInput, string>, ExperimentalDataProcessingPipeline>();
            }

            if (parametricSweepSettings is not null)
            {
                services
                    .AddSingleton(parametricSweepSettings)
                    .AddSingleton<ExecuteParametricSweepPipeline>()
                    .AddSingleton<ExecuteParametricSweepFromExperimentalDataCommand>()
                    .AddSingleton<IValidator<ExecuteParametricSweepPipelineRequest>, ExecuteParametricSweepValidator>()
                    .AddSingleton<IValidator<ExecuteParametricSweepFromExperimentalDataRequest>, ExecuteParametricSweepFromExperimentalDataValidator>()
                    .AddSingleton<IPipeline<ParametricSweepPipelineInput, string>, ParametricSweepPipeline>()
                    .AddKeyedSingleton<IParametricSweepGeneratorStep, FungParametricSweepGeneratorStep>(nameof(MechanicalModel.Fung))
                    .AddKeyedSingleton<IParametricSweepGeneratorStep, SimplifiedFungParametricSweepGeneratorStep>(nameof(MechanicalModel.SimplifiedFung))
                    .AddKeyedSingleton<IParametricSweepGeneratorStep, SchaperyParametricSweepGeneratorStep>(nameof(MechanicalModel.Schapery));
            }

            return services;
        }
    }
}
