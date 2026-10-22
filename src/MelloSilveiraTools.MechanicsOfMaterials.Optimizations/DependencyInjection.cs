using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;
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
        /// Registers the Mechanics of Materials Optimizations services
        /// (curve fitter steps, step factory, experimental data pipeline, parametric sweep pipeline, validators, and commands)
        /// into the dependency injection container.
        /// </summary>
        /// <param name="settings">
        /// Optional experimental data settings. When provided, registered as a singleton into DI.
        /// </param>
        /// <returns>The same service collection so additional registrations can be chained.</returns>
        public IServiceCollection AddOptimizationsServices(ExperimentalDataSettings? settings = null)
        {
            if (settings is not null)
            {
                services.AddSingleton(settings);
            }

            return services
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
                // Register pipelines.
                .AddScoped<IExperimentalDataProcessingPipeline, ExperimentalDataProcessingPipeline>()
                // Register Sensitivity Analysis services.
                .AddKeyedSingleton<IParametricSweepGenerator, FungParametricSweepGenerator>(nameof(MechanicalModel.Fung))
                .AddKeyedSingleton<IParametricSweepGenerator, SimplifiedFungParametricSweepGenerator>(nameof(MechanicalModel.SimplifiedFung))
                .AddKeyedSingleton<IParametricSweepGenerator, SchaperyParametricSweepGenerator>(nameof(MechanicalModel.Schapery))
                .AddScoped<ParametricSweepPipeline>()
                .AddScoped<IParametricSweepPipeline>(sp => sp.GetRequiredService<ParametricSweepPipeline>())
                // Register Request Validators.
                .AddSingleton<IValidator<ProcessExperimentalDataRequest>, ProcessExperimentalDataValidator>()
                .AddSingleton<IValidator<ExecuteParametricSweepRequest>, ExecuteParametricSweepValidator>()
                // Register Application Commands.
                .AddScoped<ProcessExperimentalDataCommand>()
                .AddScoped<ExecuteParametricSweepCommand>();
        }
    }
}
