using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;

namespace UnitTests.MechanicsOfMaterials.Commands;

public sealed class ExecuteSpecimenAnalysisValidatorTests
{
    private readonly ExecuteSpecimenAnalysisValidator _validator = new();

    private static ExecuteSpecimenAnalysisRequest CreateValidBaseRequest(JsonElement parameters) => new()
    {
        MechanicalModelName = nameof(MechanicalModel.Elastic),
        AnalysisIdentifier = "specimen-val-1",
        AnalysisParameter = new AnalysisParameter
        {
            InitialDependentMechanicalParameterValue = 0.0,
            IndependentMechanicalParameter = new MechanicalParameter(0.05)
        },
        LoadResponseRelationship = LoadResponseRelationship.StressStrain,
        ViscoelasticEffect = ViscoelasticEffect.Relaxation,
        RampTimeConsideration = RampTimeConsideration.Disregard,
        InitialTime = 0.0,
        TimeStep = 0.5,
        FinalTime = 1.0,
        AsymptoticConvergenceThreshold = 2,
        ConstitutiveParameters = parameters
    };

    [Fact]
    public async Task ValidateAsync_WhenRequestIsNull_ReturnsBadRequest()
    {
        Result result = await _validator.ValidateAsync(null!);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task ValidateAsync_WhenCoreFieldsOrEnumsAreInvalid_ReturnsAccumulatedErrors()
    {
        using JsonDocument arrayDoc = JsonDocument.Parse("[1, 2, 3]");
        ExecuteSpecimenAnalysisRequest invalid = new()
        {
            MechanicalModelName = " ",
            AnalysisParameter = null!,
            LoadResponseRelationship = (LoadResponseRelationship)999,
            ViscoelasticEffect = (ViscoelasticEffect)999,
            RampTimeConsideration = (RampTimeConsideration)999,
            InitialTime = 5.0,
            TimeStep = 0.0,
            FinalTime = 5.0,
            AsymptoticConvergenceThreshold = 0,
            ConstitutiveParameters = arrayDoc.RootElement.Clone()
        };

        Result result = await _validator.ValidateAsync(invalid);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        Assert.Contains(result.Messages, m => m.Contains("MechanicalModelName"));
        Assert.Contains(result.Messages, m => m.Contains("AnalysisParameter"));
        Assert.Contains(result.Messages, m => m.Contains("TimeStep"));
        Assert.Contains(result.Messages, m => m.Contains("FinalTime"));
        Assert.Contains(result.Messages, m => m.Contains("AsymptoticConvergenceThreshold"));
        Assert.Contains(result.Messages, m => m.Contains("ConstitutiveParameters"));
    }

    [Fact]
    public async Task ValidateAsync_WhenForceDisplacementMissingSpecimen_OrSpecimenFieldsInvalid_ReturnsBadRequest()
    {
        using JsonDocument paramsDoc = JsonDocument.Parse("""{"youngModulus": 100.0}""");

        ExecuteSpecimenAnalysisRequest missingSpecimen = CreateValidBaseRequest(paramsDoc.RootElement.Clone()) with
        {
            LoadResponseRelationship = LoadResponseRelationship.ForceDisplacement,
            Specimen = null
        };
        Result missingResult = await _validator.ValidateAsync(missingSpecimen);
        Assert.False(missingResult.Success);
        Assert.Contains(missingResult.Messages, m => m.Contains("Specimen geometry is required"));

        ExecuteSpecimenAnalysisRequest invalidSpecimen = CreateValidBaseRequest(paramsDoc.RootElement.Clone()) with
        {
            LoadResponseRelationship = LoadResponseRelationship.ForceDisplacement,
            Specimen = new SpecimenRequest
            {
                AreaInSquareMillimeters = 0.0,
                InitialLengthInMillimeters = -10.0,
                InitialAngleAsString = "invalid-vector",
                PreLoadStrain = 0.01,
                ConsiderLargeDisplacement = true,
                ConsiderAngleVariation = false
            }
        };
        Result invalidSpecResult = await _validator.ValidateAsync(invalidSpecimen);
        Assert.False(invalidSpecResult.Success);
        Assert.Contains(invalidSpecResult.Messages, m => m.Contains("AreaInSquareMillimeters"));
        Assert.Contains(invalidSpecResult.Messages, m => m.Contains("InitialLengthInMillimeters"));
        Assert.Contains(invalidSpecResult.Messages, m => m.Contains("InitialAngleAsString"));

        ExecuteSpecimenAnalysisRequest emptyAngleSpecimen = invalidSpecimen with
        {
            Specimen = invalidSpecimen.Specimen! with
            {
                AreaInSquareMillimeters = 50.0,
                InitialLengthInMillimeters = 30.0,
                InitialAngleAsString = " "
            }
        };
        Result emptyAngleResult = await _validator.ValidateAsync(emptyAngleSpecimen);
        Assert.False(emptyAngleResult.Success);
        Assert.Contains(emptyAngleResult.Messages, m => m.Contains("InitialAngleAsString is required"));
    }

    [Theory]
    [InlineData(LoadResponseRelationship.ForceDisplacement, ViscoelasticEffect.Relaxation)]
    [InlineData(LoadResponseRelationship.ForceDisplacement, ViscoelasticEffect.Creep)]
    [InlineData(LoadResponseRelationship.StressStrain, ViscoelasticEffect.Creep)]
    public async Task ExecuteSpecimenAnalysisCommand_WithSpecimenAndCreepBranches_SucceedsAndPersistsMetadata(
        LoadResponseRelationship relationship,
        ViscoelasticEffect effect)
    {
        MemoryStream captureStream = new();
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteTimebasedAsync(
                It.IsAny<string>(),
                nameof(MechanicalModel.Elastic),
                nameof(RampTimeConsideration.Disregard),
                FileExtensions.CommaSeparatedValues,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ((Stream)captureStream, "/storage/spec-branch/Elastic/Disregard.csv"));
        storageMock
            .Setup(s => s.GetMetadataAsync("/storage/spec-branch/Elastic/Disregard.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, "/storage/spec-branch/Elastic/Disregard.csv", "/storage/spec-branch/Elastic", "Disregard.csv", 128, DateTimeOffset.UtcNow));

        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.InsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(88L);

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();
        services.AddSingleton(storageMock.Object);
        services.AddSingleton(repoMock.Object);
        services.AddMathematicsServices();
        services.AddMechanicsOfMaterialsServices(addMechanicalModels: true);

        await using ServiceProvider provider = services.BuildServiceProvider();
        ExecuteSpecimenAnalysisCommand command = provider.GetRequiredService<ExecuteSpecimenAnalysisCommand>();

        using JsonDocument constitutiveJson = JsonDocument.Parse("""{"youngModulus": 100.0}""");
        ExecuteSpecimenAnalysisRequest request = CreateValidBaseRequest(constitutiveJson.RootElement.Clone()) with
        {
            AnalysisIdentifier = null,
            LoadResponseRelationship = relationship,
            ViscoelasticEffect = effect,
            Specimen = relationship == LoadResponseRelationship.ForceDisplacement
                ? new SpecimenRequest
                {
                    AreaInSquareMillimeters = 50.0,
                    InitialLengthInMillimeters = 30.0,
                    InitialAngleAsString = "1,0,0",
                    PreLoadStrain = 0.0,
                    ConsiderLargeDisplacement = false,
                    ConsiderAngleVariation = false
                }
                : null
        };

        Result<ExecuteSpecimenAnalysisResultData> result = await command.ExecuteAsync(request);

        Assert.True(result.Success, result.JoinedMessages);
        Assert.NotNull(result.Data);
        Assert.Equal(88L, result.Data.OutputMetadataEntityId);
        Assert.NotNull(result.Data.DeltaOutput);
    }
}
