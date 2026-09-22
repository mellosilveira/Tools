using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;

/// <summary>
/// Default implementation of <see cref="IMechanicalModelStepFactory"/> that resolves
/// concrete <see cref="IMechanicalModelCurveFitterStep"/> instances from the DI container
/// based on a case-insensitive model name.
/// </summary>
/// <remarks>
/// Stateless and thread-safe — suitable for singleton registration.
/// Each <see cref="Create"/> call resolves a fresh step instance from <see cref="IServiceProvider"/>.
/// </remarks>
public sealed class MechanicalModelStepFactory(IServiceProvider serviceProvider) : IMechanicalModelStepFactory
{
    /// <inheritdoc />
    public IMechanicalModelCurveFitterStep Create(string mechanicalModel, IReadOnlyList<SegmentType> targetSegments) => (mechanicalModel, targetSegments) switch
    {
        (nameof(MechanicalModel.Fung), [SegmentType.Relaxation]) => serviceProvider.GetRequiredService<FungRelaxationOnlyCurveFitterStep>(),
        (nameof(MechanicalModel.Fung), _) => serviceProvider.GetRequiredService<FungCurveFitterStep>(),

        (nameof(MechanicalModel.SimplifiedFung), [SegmentType.Relaxation]) => serviceProvider.GetRequiredService<SimplifiedFungRelaxationOnlyCurveFitterStep>(),
        (nameof(MechanicalModel.SimplifiedFung), _) => serviceProvider.GetRequiredService<SimplifiedFungCurveFitterStep>(),

        (nameof(MechanicalModel.Schapery), [SegmentType.Relaxation]) => serviceProvider.GetRequiredService<SchaperyRelaxationOnlyCurveFitterStep>(),
        (nameof(MechanicalModel.Schapery), _) => serviceProvider.GetRequiredService<SchaperyCurveFitterStep>(),

        _ => throw new ArgumentException($"Unknown mechanical model name: '{mechanicalModel}'.", nameof(mechanicalModel))
    };
}
