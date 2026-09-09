using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Abstractions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Steps;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Factories;

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
    public IMechanicalModelCurveFitterStep Create(string modelName)
    {
        string normalized = (modelName ?? string.Empty).Trim().ToUpperInvariant();

        return normalized switch
        {
            "SCHAPERY" => serviceProvider.GetRequiredService<SchaperyCurveFitterStep>(),
            "SCHAPERYRELAXATION" => serviceProvider.GetRequiredService<SchaperyRelaxationOnlyCurveFitterStep>(),
            "FUNG" => serviceProvider.GetRequiredService<FungCurveFitterStep>(),
            "FUNGRELAXATION" => serviceProvider.GetRequiredService<FungRelaxationOnlyCurveFitterStep>(),
            "SIMPLIFIEDFUNG" => serviceProvider.GetRequiredService<SimplifiedFungCurveFitterStep>(),
            "SIMPLIFIEDFUNGRELAXATION" => serviceProvider.GetRequiredService<SimplifiedFungRelaxationOnlyCurveFitterStep>(),
            _ => throw new ArgumentException($"Unknown mechanical model name: '{modelName}'.", nameof(modelName))
        };
    }
}
