using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Managers.File.Csv;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Attributes;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Files;

/// <summary>
/// Writes the mechanical model result to a CSV file.
/// </summary>
public class MechanicalModelOutputCsvFileWriter : IAsyncDisposable
{
    private readonly StringBuilder _lineBuilder = new();
    private readonly Func<MechanicalModelOutput, object>[] _compiledGetters;
    private readonly CsvFileWriter _writer;

    public MechanicalModelOutputCsvFileWriter(
        IMechanicalModelTypeResolver mechanicalModelTypeResolver,
        LoadResponseRelationship loadResponseRelationship,
        ViscoelasticEffect viscoelasticEffect,
        Stream stream)
    {
        (string header, _compiledGetters) = CreateHeaderAndGetters(mechanicalModelTypeResolver, loadResponseRelationship, viscoelasticEffect);
        _writer = new CsvFileWriter(stream, header);
    }

    public static async Task<(string Location, MechanicalModelOutputCsvFileWriter Writer)> CreateAsync(
        string uniqueIdentifier,
        IFileStorageService fileStorageService,
        IMechanicalModelTypeResolver mechanicalModelTypeResolver,
        LoadResponseRelationship loadResponseRelationship,
        ViscoelasticEffect viscoelasticEffect,
        RampTimeConsideration rampTimeConsideration)
    {
        (Stream stream, string location) = await fileStorageService.CreateWriteTimebasedAsync(uniqueIdentifier, mechanicalModelTypeResolver.MechanicalModelName, rampTimeConsideration.ToString(), FileExtensions.CommaSeparatedValues).ConfigureAwait(false);
        return (location, new(mechanicalModelTypeResolver, loadResponseRelationship, viscoelasticEffect, stream));
    }

    /// <inheritdoc/>
    public Task WriteLineAsync(MechanicalModelOutput analysisResult)
    {
        _lineBuilder.Clear();
        for (int i = 0; i < _compiledGetters.Length; i++)
        {
            if (i > 0) _lineBuilder.Append(CsvSeparators.Main);
            _lineBuilder.Append(_compiledGetters[i](analysisResult));
        }

        return _writer.WriteRowAsync(_lineBuilder.ToString());
    }

    private static (string Header, Func<MechanicalModelOutput, object>[] CompiledGetters) CreateHeaderAndGetters(IMechanicalModelTypeResolver mechanicalModelTypeResolver, LoadResponseRelationship loadResponseRelationship, ViscoelasticEffect viscoelasticEffect)
    {
        List<string> propertyNames = [];
        List<Func<MechanicalModelOutput, object>> compiledGetters = [];
        foreach (PropertyInfo property in mechanicalModelTypeResolver.Output.GetPropertiesInHierarchy())
        {
            MechanicalModelParameterAttribute? mechanicalModelParameterAttribute = property.GetCustomAttribute<MechanicalModelParameterAttribute>();
            if (mechanicalModelParameterAttribute is null || mechanicalModelParameterAttribute.CanMethodBeInvoked(loadResponseRelationship, viscoelasticEffect))
            {
                propertyNames.Add(property.Name);
                compiledGetters.Add(CompileGetter(property));
            }
        }

        return (string.Join(CsvSeparators.Main, propertyNames), [.. compiledGetters]);
    }

    /// <summary>
    /// Compiles a property getter into a delegate, replacing PropertyInfo.GetValue on every CSV line.
    /// </summary>
    private static Func<MechanicalModelOutput, object> CompileGetter(PropertyInfo property)
    {
        ParameterExpression param = Expression.Parameter(typeof(MechanicalModelOutput), "result");
        Expression access = Expression.Property(Expression.Convert(param, property.DeclaringType), property);
        Expression boxed = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<MechanicalModelOutput, object>>(boxed, param).Compile();
    }

    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
