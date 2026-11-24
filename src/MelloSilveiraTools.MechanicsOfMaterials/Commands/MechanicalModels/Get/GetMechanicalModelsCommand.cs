using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Serializers.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Commands.MechanicalModels.Get;

/// <summary>
/// Lists every <see cref="IMechanicalModelTypeResolver"/> registered in <see cref="ServiceLocator"/>
/// (both static and dynamically loaded plugin resolvers) along with its calculator type name
/// and JSON structures for constitutive parameters and output models.
/// </summary>
/// <param name="serviceLocator">Service locator resolving static and dynamic model type resolvers.</param>
public sealed class GetMechanicalModelsCommand(ServiceLocator serviceLocator) : ListedCommandBaseWithoutRequest<MechanicalModelMetadata>
{
    /// <inheritdoc/>
    protected override Task<ListedResult<MechanicalModelMetadata>> ExecuteCommandAsync(CancellationToken cancellationToken = default)
    {
        List<MechanicalModelMetadata> models =
        [
            .. serviceLocator
                .GetServices<IMechanicalModelTypeResolver>()
                .OrderBy(resolver => resolver.MechanicalModelName, StringComparer.OrdinalIgnoreCase)
                .Select(resolver => new MechanicalModelMetadata(
                    resolver.MechanicalModelName,
                    resolver.Calculator.Name,
                    MechanicsOfMaterialsJsonOptions.GetJsonSchemaAsNode(resolver.ConstitutiveParameters),
                    MechanicsOfMaterialsJsonOptions.GetJsonSchemaAsNode(resolver.Output)))
        ];

        return Task.FromResult(Result.CreateListedSuccessOk(models));
    }
}
