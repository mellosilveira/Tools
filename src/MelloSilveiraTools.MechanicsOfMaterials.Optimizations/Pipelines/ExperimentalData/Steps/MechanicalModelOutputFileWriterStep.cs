using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Managers.File.Csv;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that serializes the full array of simulated output points into a CSV file,
/// inspecting the full inheritance hierarchy of the runtime <see cref="MechanicalModelOutput"/> type
/// and compiling expression-tree property getters for zero-reflection row formatting.
/// </summary>
/// <param name="fileStorageService">Storage service responsible for resolving paths and opening file streams.</param>
/// <param name="uniqueIdentifier">Unique business identifier prefix for the generated file.</param>
public class MechanicalModelOutputFileWriterStep(IFileStorageService fileStorageService, string uniqueIdentifier)
    : IAsyncPipelineStep<MechanicalModelSimulationPayload, StorageEntryMetadata>
{
    private const string Context = "ExperimentalData";
    private static readonly ConcurrentDictionary<(Type OutputType, LoadResponseRelationship Relationship, ViscoelasticEffect Effect), (string Header, Func<MechanicalModelOutput, double?>[] Getters)> SchemaCache = new();

    /// <inheritdoc />
    public string Name => nameof(MechanicalModelOutputFileWriterStep);

    /// <inheritdoc />
    public async Task<StorageEntryMetadata> ExecuteAsync(MechanicalModelSimulationPayload payload, CancellationToken cancellationToken = default)
    {
        MechanicalModelOutput[] points = payload.SimulatedPoints;
        Type outputType = points.Length > 0 ? points[0].GetType() : typeof(MechanicalModelOutput);

        (string header, Func<MechanicalModelOutput, double?>[] getters) = SchemaCache.GetOrAdd(
            (outputType, payload.CurveFit.LoadResponseRelationship, payload.CurveFit.ViscoelasticEffect),
            BuildSchema);

        string filePrefix = $"{payload.CurveFit.MechanicalModelName}_{payload.CurveFit.RampTimeConsideration}";
        (Stream stream, string location) = await fileStorageService.CreateWriteTimebasedAsync(
            Context,
            uniqueIdentifier,
            filePrefix,
            FileExtensions.CommaSeparatedValues,
            cancellationToken).ConfigureAwait(false);

        await using (stream)
        await using (CsvFileWriter session = new(stream, header))
        {
            for (int i = 0; i < points.Length; i++)
            {
                MechanicalModelOutput point = points[i];
                await session.WriteRowAsync(buffer =>
                {
                    for (int p = 0; p < getters.Length; p++)
                    {
                        if (p > 0)
                            buffer.Append(CsvSeparators.Main);

                        double? val = getters[p](point);
                        if (val.HasValue)
                            buffer.Append(val.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        return await fileStorageService.GetMetadataAsync(location, cancellationToken).ConfigureAwait(false);
    }

    private static (string Header, Func<MechanicalModelOutput, double?>[] Getters) BuildSchema(
        (Type OutputType, LoadResponseRelationship Relationship, ViscoelasticEffect Effect) key)
    {
        PropertyInfo[] properties = key.OutputType.GetPropertiesInHierarchy();
        List<PropertyInfo> matching = new(properties.Length);

        for (int i = 0; i < properties.Length; i++)
        {
            PropertyInfo property = properties[i];
            MechanicalModelParameterAttribute? attr = property.GetCustomAttribute<MechanicalModelParameterAttribute>();
            if (attr is null || attr.CanMethodBeInvoked(key.Relationship, key.Effect))
                matching.Add(property);
        }

        Func<MechanicalModelOutput, double?>[] getters = new Func<MechanicalModelOutput, double?>[matching.Count];
        StringBuilder headerBuilder = new();

        for (int i = 0; i < matching.Count; i++)
        {
            if (i > 0)
                headerBuilder.Append(CsvSeparators.Main);

            headerBuilder.Append(matching[i].Name);
            getters[i] = CompileGetter(matching[i]);
        }

        return (headerBuilder.ToString(), getters);
    }

    private static Func<MechanicalModelOutput, double?> CompileGetter(PropertyInfo property)
    {
        ParameterExpression param = Expression.Parameter(typeof(MechanicalModelOutput), "output");
        MemberExpression access = Expression.Property(Expression.Convert(param, property.DeclaringType!), property);
        UnaryExpression converted = Expression.Convert(access, typeof(double?));
        return Expression.Lambda<Func<MechanicalModelOutput, double?>>(converted, param).Compile();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
