using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Services;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;
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
        public IServiceCollection AddOptimizationsServices(ExperimentalDataSettings? settings = null, CurveFitterOptions? curveFitterOptions = null)
        {
            if (settings is not null)
            {
                services.AddSingleton(settings);
            }

            if (curveFitterOptions is not null)
            {
                services.AddSingleton(curveFitterOptions);
            }

            return services
                // Register Step Execution Manager.
                .AddSingleton<IStepExecutionManager, StepExecutionManager>()
                // Register Curve Fitter implementations.
                .AddKeyedSingleton<ICurveFitter, AlglibCurveFitter>(CurveFitterType.Alglib)
                .AddKeyedSingleton<ICurveFitter, MathNetCurveFitter>(CurveFitterType.MathNet)
                // Register MathExpression curve fitter decorators.
                .AddKeyedSingleton<IMathExpressionCurveFitter, PronySeriesCurveFitter>(MathExpressionType.PronySeries)
                // Register MathematicalFunction curve fitter decorators.
                .AddKeyedSingleton<IFunctionCurveFitter, LogarithmicCurveFitter>(FunctionType.Logarithmic)
                .AddKeyedSingleton<IFunctionCurveFitter, ExponentialCurveFitter>(FunctionType.Exponential)
                .AddKeyedSingleton<IFunctionCurveFitter, PolynomialCurveFitter>(FunctionType.Polynomial)
                // Register the step factory.
                .AddSingleton<IMechanicalModelStepFactory, MechanicalModelStepFactory>()
                // Register Sensitivity Analysis services.
                .AddKeyedSingleton<IParametricSweepGenerator, FungParametricSweepGenerator>(nameof(MechanicalModel.Fung))
                .AddKeyedSingleton<IParametricSweepGenerator, SimplifiedFungParametricSweepGenerator>(nameof(MechanicalModel.SimplifiedFung))
                .AddKeyedSingleton<IParametricSweepGenerator, SchaperyParametricSweepGenerator>(nameof(MechanicalModel.Schapery))
                // Register pipelines.
                .AddSingleton<IPipeline<ExperimentalDataProcessingInput, string>, ExperimentalDataProcessingPipeline>()
                .AddSingleton<IPipeline<ParametricSweepInput, string>, ParametricSweepPipeline>()
                .AddSingleton<IPipeline<VariableRelationshipInput, VariableRelationshipOutput>, VariableRelationshipPipeline>()
                // Register Request Validators.
                .AddSingleton<IValidator<ProcessExperimentalDataRequest>, ProcessExperimentalDataValidator>()
                .AddSingleton<IValidator<ExecuteParametricSweepRequest>, ExecuteParametricSweepValidator>()
                // Register Application Commands.
                .AddScoped<ProcessExperimentalDataCommand>()
                .AddScoped<ResumeExperimentalDataStepCommand>()
                .AddScoped<ExecuteParametricSweepCommand>()
                .AddScoped<ExecuteVariableRelationshipCommand>();
        }
    }
}
