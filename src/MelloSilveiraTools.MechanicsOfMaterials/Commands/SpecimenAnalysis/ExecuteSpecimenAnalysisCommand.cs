using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.Converters;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Files;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Serializers.Json;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;

/// <summary>
/// Executes a single deterministic specimen mechanical analysis for any statically registered or
/// dynamically loaded plugin constitutive model, persists the discretized time-history CSV through
/// <see cref="IFileStorageService"/>, and returns initial/final boundary values and asymptotic convergence metadata.
/// </summary>
/// <param name="logger">Structured logger for specimen analysis execution.</param>
/// <param name="validator">Validator enforcing request invariants prior to numerical integration.</param>
/// <param name="calculatorFactory">Factory that constructs the compiled <see cref="IMechanicalModelCalculatorFacade"/> for the resolved model.</param>
/// <param name="fileStorageService">Default file storage service used to persist generated CSV outputs.</param>
/// <param name="repository">Repository for managing storage entry metadata.</param>
/// <param name="serviceLocator">Service locator used to resolve the keyed <see cref="IMechanicalModelTypeResolver"/> and <see cref="IFileStorageService"/>.</param>
public sealed class ExecuteSpecimenAnalysisCommand(
    ILogger<ExecuteSpecimenAnalysisCommand> logger,
    IValidator<ExecuteSpecimenAnalysisRequest> validator,
    IMechanicalModelCalculatorFactory calculatorFactory,
    IFileStorageService fileStorageService,
    IRepository repository,
    ServiceLocator serviceLocator)
    : CommandBaseWithData<ExecuteSpecimenAnalysisRequest, ExecuteSpecimenAnalysisResultData>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<ExecuteSpecimenAnalysisResultData>> ExecuteCommandAsync(ExecuteSpecimenAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        IMechanicalModelTypeResolver? typeResolver = serviceLocator.GetKeyedService<IMechanicalModelTypeResolver>(request.MechanicalModelName);
        if (typeResolver is null)
            return Result.CreateNotFound($"Mechanical model '{request.MechanicalModelName}' is not registered.");

        ConstitutiveParameters? constitutiveParameters = MechanicsOfMaterialsJsonOptions.SafeDeserialize<ConstitutiveParameters>(logger, request.ConstitutiveParameters, typeResolver.ConstitutiveParameters);
        if (constitutiveParameters is null)
            return Result.CreateBadRequest($"Unable to deserialize ConstitutiveParameters for model '{request.MechanicalModelName}'.");

        string identifier = string.IsNullOrWhiteSpace(request.AnalysisIdentifier) ? Guid.NewGuid().ToString("N") : request.AnalysisIdentifier;
        GenericMechanicalModelInput input = BuildInput(request, identifier, constitutiveParameters);
        IMechanicalModelCalculatorFacade calculatorFacade = calculatorFactory.CreateCalculatorFacade(typeResolver, input);

        double initialTime = request.InitialTime ?? MathematicConstants.InitialTime;
        (string location, MechanicalModelOutputCsvFileWriter fileWriter) = await MechanicalModelOutputCsvFileWriter
            .CreateAsync(identifier, fileStorageService, typeResolver, request.LoadResponseRelationship, request.ViscoelasticEffect, request.RampTimeConsideration)
            .ConfigureAwait(false);

        MechanicalModelOutput initialOutput;
        MechanicalModelOutput lastOutput;
        double? asymptoteTime = null;
        int asymptoteCount = 0;

        await using (fileWriter)
        {
            lastOutput = initialOutput = calculatorFacade.Calculate(initialTime);
            await fileWriter.WriteLineAsync(initialOutput).ConfigureAwait(false);

            int totalSteps = (int)Math.Round((request.FinalTime - initialTime) / request.TimeStep);
            for (int i = 1; i <= totalSteps; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                double time = initialTime + i * request.TimeStep;
                MechanicalModelOutput output = calculatorFacade.Calculate(time);

                if (HasReachedAsymptote(lastOutput, output, request.AsymptoticConvergenceThreshold, ref asymptoteCount))
                    asymptoteTime ??= time;

                await fileWriter.WriteLineAsync(output).ConfigureAwait(false);
                lastOutput = output;
            }
        }

        StorageEntryMetadata outputFileMetadata = await fileStorageService.GetMetadataAsync(location, cancellationToken).ConfigureAwait(false);
        long outputMetadataEntityId = await repository.InsertAsync(StorageEntryMetadataEntity.Create(outputFileMetadata, identifier), cancellationToken).ConfigureAwait(false);

        SimulationDeltaOutput deltaOutput = SimulationDeltaOutput.Calculate(typeResolver, initialOutput, lastOutput);
        return new ExecuteSpecimenAnalysisResultData(deltaOutput, asymptoteTime, outputMetadataEntityId);
    }

    private static GenericMechanicalModelInput BuildInput(ExecuteSpecimenAnalysisRequest request, string identifier, ConstitutiveParameters constitutiveParameters)
    {
        SpecimenParameter? specimen = request.Specimen is null
            ? null
            : new SpecimenParameter(
                considerLargeDisplacement: request.Specimen.ConsiderLargeDisplacement ?? false,
                considerAngleVariation: request.Specimen.ConsiderAngleVariation ?? false,
                preLoadStrain: request.Specimen.PreLoadStrain,
                preLoadLength: UnitConverter.ConvertMmToM(request.Specimen.InitialLengthInMillimeters),
                area: UnitConverter.ConvertMm2ToM2(request.Specimen.AreaInSquareMillimeters),
                initialAngle: Vector3D.Parse(request.Specimen.InitialAngleAsString));

        MechanicalParameter independentParameter = request.AnalysisParameter.IndependentMechanicalParameter;
        MechanicalParameter dependentParameter = new(request.AnalysisParameter.InitialDependentMechanicalParameterValue);

        GenericMechanicalModelInput baseInput = new()
        {
            Identifier = identifier,
            MechanicalModelName = request.MechanicalModelName,
            AcceptedStrainRange = request.AcceptedRange ?? AcceptedRange.Default,
            LoadResponseRelationship = request.LoadResponseRelationship,
            ViscoelasticEffect = request.ViscoelasticEffect,
            RampTimeConsideration = request.RampTimeConsideration,
            RampTime = request.RampTime,
            TimeStep = request.TimeStep,
            Specimen = specimen,
            ConstitutiveParameters = constitutiveParameters
        };

        return (request.LoadResponseRelationship, request.ViscoelasticEffect) switch
        {
            (LoadResponseRelationship.StressStrain, ViscoelasticEffect.Relaxation) => baseInput with { Strain = independentParameter, Stress = dependentParameter },
            (LoadResponseRelationship.StressStrain, ViscoelasticEffect.Creep) => baseInput with { Strain = dependentParameter, Stress = independentParameter },
            (LoadResponseRelationship.ForceDisplacement, ViscoelasticEffect.Relaxation) => baseInput with { Displacement = independentParameter, Force = dependentParameter },
            (LoadResponseRelationship.ForceDisplacement, ViscoelasticEffect.Creep) => baseInput with { Displacement = dependentParameter, Force = independentParameter },
            _ => throw new ArgumentOutOfRangeException(nameof(request), $"Unsupported combination: {request.LoadResponseRelationship} and {request.ViscoelasticEffect}.")
        };
    }

    private static bool HasReachedAsymptote(MechanicalModelOutput lastOutput, MechanicalModelOutput currentOutput, int threshold, ref int asymptoteCount)
    {
        asymptoteCount = lastOutput == currentOutput ? asymptoteCount + 1 : 1;
        return asymptoteCount == threshold;
    }
}
