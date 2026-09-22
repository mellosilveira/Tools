using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations;

/// <summary>
/// Provides extension methods to register Optimizations services
/// (experimental data processing pipeline, curve fitter steps, and factory) into the DI container.
/// </summary>
public static class OptimizationsDependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Mechanics of Materials Optimizations services
        /// (curve fitter steps, step factory, and experimental data service)
        /// into the dependency injection container.
        /// </summary>
        /// <param name="settings">
        /// Optional experimental data settings. When <see langword="null"/>, a default instance is registered.
        /// </param>
        /// <returns>The same service collection so additional registrations can be chained.</returns>
        public IServiceCollection AddOptimizationsServices(ExperimentalDataSettings? settings = null)
        {
            return services
                // Register settings.
                .AddSingleton(settings ?? new ExperimentalDataSettings())
                // Register curve fitter steps as singletons (stateless, thread-safe).
                .AddSingleton<SchaperyCurveFitterStep>()
                .AddSingleton<SchaperyRelaxationOnlyCurveFitterStep>()
                .AddSingleton<FungCurveFitterStep>()
                .AddSingleton<FungRelaxationOnlyCurveFitterStep>()
                .AddSingleton<SimplifiedFungCurveFitterStep>()
                .AddSingleton<SimplifiedFungRelaxationOnlyCurveFitterStep>()
                // Register Curve Fitter implementations.
                .AddSingleton<ICurveFitter, AlglibCurveFitter>()
                // Register MathExpression curve fitter decorators.
                .AddKeyedSingleton<IMathExpressionCurveFitter, PronySeriesCurveFitter>(MathExpressionType.PronySeries)
                // Register MathematicalFunction curve fitter decorators.
                .AddKeyedSingleton<IMathematicalFunctionCurveFitter, LogarithmicCurveFitter>(FunctionType.Logarithmic)
                .AddKeyedSingleton<IMathematicalFunctionCurveFitter, ExponentialCurveFitter>(FunctionType.Exponential)
                .AddKeyedSingleton<IMathematicalFunctionCurveFitter, PolynomialCurveFitter>(FunctionType.Polynomial)
                // Register the step factory.
                .AddSingleton<IMechanicalModelStepFactory, MechanicalModelStepFactory>()
                // Register the experimental data service.
                .AddScoped<IExperimentalDataService, ExperimentalDataService>();
        }
    }
}
