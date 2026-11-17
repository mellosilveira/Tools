using System.Text;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Factories.Functions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter.Fung;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Unit tests for experimental data processing steps: segmentation, downsampling, curve segment building,
/// CSV writing, identifier hashing, persistence steps, simulation step, and curve fitter step edge branches.
/// </summary>
public sealed class ExperimentalDataStepsTests
{
    /// <summary>
    /// Verifies ExperimentalDataSegmenterStep classifies Ramp, Relaxation, Descent, and Recovery segments,
    /// skips below-threshold/mismatched/non-positive rows, flushes buffers, and appends a terminal Unknown point.
    /// </summary>
    [Fact]
    public async Task ExperimentalDataSegmenterStep_ClassifiesAllFourSegmentTypesAndAppendsTerminalUnknown()
    {
        StringBuilder strainCsv = new("Time,Strain\n");
        StringBuilder stressCsv = new("Time,Stress\n");

        // Pre-threshold (t < 1.0), mismatched stress time, and non-positive strain (filtered out)
        strainCsv.Append("0.5,0.01\n1.0,0.01\n1.1,0.0\n");
        stressCsv.Append("0.5,1.0\n9.9,1.0\n1.1,1.0\n");

        // Segment 1: Ramp (t = 2..5, strain 0.01->0.04, stress 1->3.2)
        double[] rampStress = [1.0, 2.0, 3.0, 3.2];
        for (int i = 0; i < rampStress.Length; i++)
        {
            double t = 2.0 + i;
            strainCsv.Append($"{t.ToString(System.Globalization.CultureInfo.InvariantCulture)},{(0.01 * (i + 1)).ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");
            stressCsv.Append($"{t.ToString(System.Globalization.CultureInfo.InvariantCulture)},{rampStress[i].ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");
        }

        // Segment 2: Relaxation (t = 6..9, strain constant 0.04, stress convex decay: 3.2, 3.05, 2.95, 2.90)
        double[] relaxStress = [3.2, 3.05, 2.95, 2.90];
        for (int i = 0; i < relaxStress.Length; i++)
        {
            double t = 6.0 + i;
            strainCsv.Append($"{t.ToString(System.Globalization.CultureInfo.InvariantCulture)},0.04\n");
            stressCsv.Append($"{t.ToString(System.Globalization.CultureInfo.InvariantCulture)},{relaxStress[i].ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");
        }

        // Segment 3: Descent (t = 10..13, strain 0.04->0.01, stress 2.90->1.3)
        double[] descentStress = [2.90, 2.2, 1.7, 1.3];
        for (int i = 0; i < descentStress.Length; i++)
        {
            double t = 10.0 + i;
            strainCsv.Append($"{t.ToString(System.Globalization.CultureInfo.InvariantCulture)},{(0.04 - 0.01 * i).ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");
            stressCsv.Append($"{t.ToString(System.Globalization.CultureInfo.InvariantCulture)},{descentStress[i].ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");
        }

        // Segment 4: Recovery (t = 14..17, strain constant 0.01, stress concave recovery: 1.3, 1.6, 1.8, 1.9)
        double[] recoveryStress = [1.3, 1.6, 1.8, 1.9];
        for (int i = 0; i < recoveryStress.Length; i++)
        {
            double t = 14.0 + i;
            strainCsv.Append($"{t.ToString(System.Globalization.CultureInfo.InvariantCulture)},0.01\n");
            stressCsv.Append($"{t.ToString(System.Globalization.CultureInfo.InvariantCulture)},{recoveryStress[i].ToString(System.Globalization.CultureInfo.InvariantCulture)}\n");
        }

        Mock<IFileStorageService> storageMock = new();
        storageMock.Setup(s => s.OpenReadAsync("strain.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(Encoding.UTF8.GetBytes(strainCsv.ToString())));
        storageMock.Setup(s => s.OpenReadAsync("stress.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(Encoding.UTF8.GetBytes(stressCsv.ToString())));

        await using ExperimentalDataSegmenterStep step = new(
            NullLogger<ExperimentalDataSegmenterStep>.Instance,
            new Differentiation(),
            storageMock.Object);

        Assert.Equal("ExperimentalDataSegmenter", step.Name);

        ExperimentalDataSegmenterInput input = new(
            StartExperimentalTimeThreshold: 1.0,
            StrainFileLocation: "strain.csv",
            StressFileLocation: "stress.csv",
            Options: new ExperimentalDataProcessingOptions(BufferSize: 4, AccelerationTolerance: 0.5));

        List<SegmentedDataPoint> output = [];
        await foreach (SegmentedDataPoint point in step.ExecuteAsync(input, CancellationToken.None))
            output.Add(point);

        Assert.NotEmpty(output);
        Assert.Equal(SegmentType.Unknown, output[^1].SegmentType);
        Assert.Contains(output, p => p.SegmentType == SegmentType.Ramp);
        Assert.Contains(output, p => p.SegmentType == SegmentType.Relaxation);
        Assert.Contains(output, p => p.SegmentType == SegmentType.Descent);
        Assert.Contains(output, p => p.SegmentType == SegmentType.Recovery);
    }

    /// <summary>
    /// Verifies ExperimentalDataSegmenterStep throws InvalidOperationException on incoherent strain pattern
    /// where decreasing strain occurs in a non-contiguous/invalid configuration during buffer slicing.
    /// </summary>
    [Fact]
    public async Task ExperimentalDataSegmenterStep_InvalidStrainPattern_ThrowsInvalidOperationException()
    {
        // Decreasing timestamps cause positive strain rise with negative dt -> negative strainRate -> SliceBuffer(maxStrainIndex > minStrainIndex)
        string strainCsv = "Time,Strain\n4.0,0.01\n3.0,0.02\n2.0,0.03\n1.0,0.04\n";
        string stressCsv = "Time,Stress\n4.0,1.0\n3.0,2.0\n2.0,3.0\n1.0,4.0\n";

        Mock<IFileStorageService> storageMock = new();
        storageMock.Setup(s => s.OpenReadAsync("s1.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(Encoding.UTF8.GetBytes(strainCsv)));
        storageMock.Setup(s => s.OpenReadAsync("s2.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(Encoding.UTF8.GetBytes(stressCsv)));

        await using ExperimentalDataSegmenterStep step = new(
            NullLogger<ExperimentalDataSegmenterStep>.Instance,
            new Differentiation(),
            storageMock.Object);

        ExperimentalDataSegmenterInput input = new(
            0.0,
            "s1.csv",
            "s2.csv",
            new ExperimentalDataProcessingOptions(BufferSize: 4));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (SegmentedDataPoint _ in step.ExecuteAsync(input, CancellationToken.None))
            {
            }
        });
    }

    /// <summary>
    /// Verifies ExperimentalDataDownsamplerStep and CurveSegmentBuilderStep filter, group, and emit segments accurately.
    /// </summary>
    [Fact]
    public async Task DownsamplerAndCurveSegmentBuilder_FilterAndGroupCorrectly()
    {
        await using ExperimentalDataDownsamplerStep downsampler = new(skipTimeStep: 2.0);
        await using CurveSegmentBuilderStep builder = new();

        Assert.Equal(nameof(ExperimentalDataDownsamplerStep), downsampler.Name);
        Assert.Equal(nameof(CurveSegmentBuilderStep), builder.Name);

        SegmentedDataPoint[] raw =
        [
            new(SegmentType.Ramp, new ProcessedDataPoint(0.0, 0.01, 0.01, 0, 1.0, 1.0, 0)),
            new(SegmentType.Ramp, new ProcessedDataPoint(1.0, 0.02, 0.01, 0, 2.0, 1.0, 0)), // skipped by 2.0 step
            new(SegmentType.Ramp, new ProcessedDataPoint(2.0, 0.03, 0.01, 0, 3.0, 1.0, 0)),
            new(SegmentType.Relaxation, new ProcessedDataPoint(4.0, 0.03, 0, 0, 2.5, -0.1, 0.01)),
            new(SegmentType.Unknown, default)
        ];

        List<CurveSegment> segments = [];
        foreach (SegmentedDataPoint p in raw)
        {
            await foreach (SegmentedDataPoint ds in downsampler.ExecuteAsync(p, CancellationToken.None))
            {
                await foreach (CurveSegment seg in builder.ExecuteAsync(ds, CancellationToken.None))
                    segments.Add(seg);
            }
        }

        Assert.Equal(2, segments.Count);
        Assert.Equal(SegmentType.Ramp, segments[0].Type);
        Assert.Equal([0.0, 2.0], segments[0].TimePoints);
        Assert.Equal(SegmentType.Relaxation, segments[1].Type);
        Assert.Equal([4.0], segments[1].TimePoints);
    }

    /// <summary>
    /// Verifies ExperimentalDataFileWriterStep creates storage streams and writes formatted CSV rows.
    /// </summary>
    [Fact]
    public async Task ExperimentalDataFileWriterStep_WritesHeaderAndRows()
    {
        await using MemoryStream buffer = new();
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteTimebasedAsync("ExperimentalData", "pipe-1", FileExtensions.CommaSeparatedValues, It.IsAny<CancellationToken>()))
            .ReturnsAsync((buffer, "storage/ExperimentalData/pipe-1.csv"));

        (string location, ExperimentalDataFileWriterStep step) = await ExperimentalDataFileWriterStep.CreateAsync(storageMock.Object, "pipe-1");
        Assert.Equal("storage/ExperimentalData/pipe-1.csv", location);
        Assert.Equal(location, step.Location);
        Assert.Equal(nameof(ExperimentalDataFileWriterStep), step.Name);

        await step.ExecuteAsync(new SegmentedDataPoint(SegmentType.Ramp, new ProcessedDataPoint(1.0, 0.02, 0.01, 0.0, 5.0, 2.0, 0.0)));
        await step.DisposeAsync();

        string written = Encoding.UTF8.GetString(buffer.ToArray());
        Assert.Contains("Time,Strain,Stress,SegmentType,StrainRate,StrainAcceleration,StressRate,StressAcceleration", written);
        Assert.Contains("1,0.02,5,1", written);
    }

    /// <summary>
    /// Verifies CurveFitOutputIdentifierBuilderStep, CurveFitOutputPersistenceStep, and MechanicalModelOutputPersistenceStep
    /// handle success, conflict, and error repository results.
    /// </summary>
    [Fact]
    public async Task PersistenceAndIdentifierSteps_HandleSuccessConflictAndError()
    {
        using CurveFitOutputIdentifierBuilderStep idBuilder = new();
        Assert.Equal(nameof(CurveFitOutputIdentifierBuilderStep), idBuilder.Name);

        ElasticConstitutiveParameters elasticParams = new() { YoungModulus = 100.0 };
        MechanicalModelCurveFitOutput fitOutput = new(
            [],
            elasticParams,
            new AcceptedRange(0.0, 0.05),
            LoadResponseRelationship.StressStrain,
            ViscoelasticEffect.Relaxation,
            RampTimeConsideration.Disregard,
            0.99,
            0.001,
            5,
            []);

        string id1 = idBuilder.Execute(fitOutput);
        string id2 = idBuilder.Execute(fitOutput);
        Assert.Equal(64, id1.Length);
        Assert.Equal(id1, id2);

        Mock<IRepository> repoMock = new();
        repoMock.SetupSequence(r => r.TryInsertAsync(It.IsAny<MechanicalModelCurveFitOutputEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L))
            .ReturnsAsync(Result.CreateConflict(0L, "dup"))
            .ReturnsAsync(Result.CreateError(StatusCode.BadRequest, "err"));

        await using CurveFitOutputPersistenceStep fitPersistence = new(
            NullLogger<CurveFitOutputPersistenceStep>.Instance,
            repoMock.Object,
            "pipe-1",
            nameof(MechanicalModel.Elastic));

        Assert.Equal(nameof(CurveFitOutputPersistenceStep), fitPersistence.Name);
        await fitPersistence.ExecuteAsync((id1, fitOutput));
        await fitPersistence.ExecuteAsync((id1, fitOutput));
        await fitPersistence.ExecuteAsync((id1, fitOutput));

        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.GetMetadataAsync("exp.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, "exp.csv", "dir", "exp.csv", 123, DateTimeOffset.UtcNow));

        repoMock.SetupSequence(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(10L))
            .ReturnsAsync(Result.CreateConflict(0L, "dup-file"))
            .ReturnsAsync(Result.CreateError(StatusCode.BadRequest, "err-file"))
            .ReturnsAsync(Result.CreateSuccessOk(11L))
            .ReturnsAsync(Result.CreateSuccessOk(12L))
            .ReturnsAsync(Result.CreateSuccessOk(13L));

        repoMock.SetupSequence(r => r.TryInsertAsync(It.IsAny<MechanicalModelSimulationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(20L))
            .ReturnsAsync(Result.CreateConflict(0L, "dup-sim"))
            .ReturnsAsync(Result.CreateError(StatusCode.BadRequest, "err-sim"));

        await using MechanicalModelOutputPersistenceStep simPersistence = new(
            NullLogger<MechanicalModelOutputPersistenceStep>.Instance,
            storageMock.Object,
            repoMock.Object,
            "pipe-1",
            nameof(MechanicalModel.Elastic));

        Assert.Equal(nameof(MechanicalModelOutputPersistenceStep), simPersistence.Name);

        SimulationDeltaOutput delta = new(
            new ElasticModelOutput { Stress = 10.0 },
            new ElasticModelOutput { Stress = 10.0 },
            new ElasticModelOutput { Stress = 0.0 },
            new ElasticModelOutput { Stress = 0.0 });
        MechanicalModelOutputPersistenceInput simInput = new(
            "exp.csv",
            id1,
            2.5,
            delta,
            new StorageEntryMetadata(StorageProvider.Local, "sim.csv", "dir", "sim.csv", 456, DateTimeOffset.UtcNow));

        string hash1 = await simPersistence.ExecuteAsync(simInput);
        string hash2 = await simPersistence.ExecuteAsync(simInput);
        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);

        repoMock.Verify(r => r.TryInsertAsync(It.IsAny<MechanicalModelSimulationEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        await simPersistence.ExecuteAsync(simInput with { AsymptoteTime = null });
    }

    /// <summary>
    /// Verifies MechanicalModelSimulationStep runs forward simulation across Ramp and Relaxation segments,
    /// extrapolates to finalSimulationTime, detects asymptotes, and writes simulation CSV output.
    /// </summary>
    [Fact]
    public async Task MechanicalModelSimulationStep_SimulatesAndExtrapolatesWithAsymptoteDetection()
    {
        await using MemoryStream csvBuffer = new();
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteTimebasedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((csvBuffer, "sim-out.csv"));
        storageMock
            .Setup(s => s.GetMetadataAsync("sim-out.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, "sim-out.csv", "dir", "sim-out.csv", 100, DateTimeOffset.UtcNow));

        Mock<IMechanicalModelTypeResolver> resolverMock = new();
        resolverMock.SetupGet(r => r.MechanicalModelName).Returns(nameof(MechanicalModel.Elastic));
        resolverMock.SetupGet(r => r.Output).Returns(typeof(MechanicalModelOutput));
        resolverMock.SetupGet(r => r.ConstitutiveParameters).Returns(typeof(ElasticConstitutiveParameters));
        IMechanicalModelTypeResolver resolver = resolverMock.Object;

        Mock<IMechanicalModelCalculatorFacade> facadeMock = new();
        facadeMock
            .Setup(f => f.Calculate(It.IsAny<double>()))
            .Returns(new MechanicalModelOutput { Stress = 5.0 });

        Mock<IMechanicalModelCalculatorFactory> factoryMock = new();
        factoryMock
            .Setup(f => f.CreateCalculatorFacade(resolver, It.IsAny<GenericMechanicalModelInput>()))
            .Returns(facadeMock.Object);

        await using MechanicalModelSimulationStep step = new(
            storageMock.Object,
            resolver,
            factoryMock.Object,
            "pipe-sim",
            simulationTimeStep: null,
            finalSimulationTime: 5.0,
            asymptoteConsecutivePointsThreshold: 2);

        Assert.Equal(nameof(MechanicalModelSimulationStep), step.Name);

        CurveSegment ramp = new()
        {
            Type = SegmentType.Ramp,
            TimePoints = [0.0, 1.0],
            ExperimentalStrain = [0.0, 0.05],
            ExperimentalStress = [0.0, 5.0]
        };
        CurveSegment relaxation = new()
        {
            Type = SegmentType.Relaxation,
            TimePoints = [1.0, 2.0, 3.0],
            ExperimentalStrain = [0.05, 0.05, 0.05],
            ExperimentalStress = [5.0, 5.0, 5.0]
        };

        MechanicalModelCurveFitOutput fitOutput = new(
            [ramp, relaxation],
            new ElasticConstitutiveParameters { YoungModulus = 100.0 },
            new AcceptedRange(0.0, 0.05),
            LoadResponseRelationship.StressStrain,
            ViscoelasticEffect.Relaxation,
            RampTimeConsideration.Disregard,
            1.0,
            0.0,
            1,
            []);

        (double? asymptoteTime, SimulationDeltaOutput delta, StorageEntryMetadata meta) = await step.ExecuteAsync(fitOutput);

        Assert.NotNull(asymptoteTime);
        Assert.Equal("sim-out.csv", meta.Location);
        Assert.Equal(0.0, delta.AbsoluteDelta.Stress);
    }

    /// <summary>
    /// Verifies remaining edge branches in Fung, SimplifiedFung, and Schapery curve fitter steps:
    /// relaxation-only without preceding ramp, non-ramp/non-relaxation segment reset,
    /// empty Schapery Unknown segment, single-strain Helmholtz branch, and Helmholtz all-failed exception.
    /// </summary>
    [Fact]
    public async Task CurveFitterSteps_EdgeBranches_ExecuteCorrectly()
    {
        Mock<ICurveFitter> fitterMock = new();
        fitterMock.Setup(f => f.Fit(It.Is<CurveFitInput>(i => i.InitialParameters.Length == 3)))
            .Returns((CurveFitInput i) =>
            {
                _ = i.Calculate(i.InitialParameters, i.IndependentVariables[0]);
                _ = i.ValidateParameters?.Invoke([0.5, 1.0, 10.0]);
                return new CurveFitOutput([new(0, 0.5, 0.4, 0.6, 0.01, 0.95), new(1, 1.0, 0.8, 1.2, 0.01, 0.95), new(2, 10.0, 8.0, 12.0, 0.1, 0.95)], 0.01, 0.98, 5);
            });
        fitterMock.Setup(f => f.Fit(It.Is<CurveFitInput>(i => i.InitialParameters.Length == 7)))
            .Returns(new CurveFitOutput([.. Enumerable.Range(0, 7).Select(idx => new ParameterConfidenceInterval(idx, 0.1, 0.0, 0.2, 0.01, 0.95))], 0.01, 0.98, 5));
        fitterMock.Setup(f => f.Fit(It.Is<CurveFitInput>(i => i.InitialParameters.Length == 2)))
            .Returns((CurveFitInput i) =>
            {
                _ = i.Calculate(i.InitialParameters, i.IndependentVariables[0]);
                return new CurveFitOutput([new(0, 1.1, 1.0, 1.2, 0.01, 0.95), new(1, 0.9, 0.8, 1.0, 0.01, 0.95)], 0.01, 0.97, 4);
            });

        CurveSegment ramp = new()
        {
            Type = SegmentType.Ramp,
            TimePoints = [0.0, 1.0],
            ExperimentalStrain = [0.0, 0.03],
            ExperimentalStress = [0.0, 1.0]
        };
        CurveSegment descent = new()
        {
            Type = SegmentType.Descent,
            TimePoints = [1.0, 2.0],
            ExperimentalStrain = [0.03, 0.01],
            ExperimentalStress = [1.0, 0.2]
        };
        CurveSegment relaxation = new()
        {
            Type = SegmentType.Relaxation,
            TimePoints = [2.0, 3.0, 4.0],
            ExperimentalStrain = [0.03, 0.03, 0.03],
            ExperimentalStress = [1.0, 0.8, 0.7]
        };

        Mock<IFungModelCalculator> fungCalcMock = new();
        await using FungRelaxationOnlyCurveFitterStep fungStep = new(fungCalcMock.Object, fitterMock.Object, 0.90);
        // Ramp followed by Descent clears _currentRamp so next Relaxation hits else branch
        await foreach (MechanicalModelCurveFitOutput _ in fungStep.ExecuteAsync(ramp)) { }
        await foreach (MechanicalModelCurveFitOutput _ in fungStep.ExecuteAsync(descent)) { }

        List<MechanicalModelCurveFitOutput> fungOutputs = [];
        await foreach (MechanicalModelCurveFitOutput item in fungStep.ExecuteAsync(relaxation))
            fungOutputs.Add(item);
        Assert.Single(fungOutputs);

        Mock<ISimplifiedFungModelCalculator> simplifiedCalcMock = new();
        await using SimplifiedFungRelaxationOnlyCurveFitterStep simplifiedStep = new(
            simplifiedCalcMock.Object,
            fitterMock.Object,
            new PronySeriesCurveFitter(fitterMock.Object),
            0.90);
        await foreach (MechanicalModelCurveFitOutput _ in simplifiedStep.ExecuteAsync(ramp)) { }
        await foreach (MechanicalModelCurveFitOutput _ in simplifiedStep.ExecuteAsync(descent)) { }

        List<MechanicalModelCurveFitOutput> simplifiedOutputs = [];
        await foreach (MechanicalModelCurveFitOutput item in simplifiedStep.ExecuteAsync(relaxation))
            simplifiedOutputs.Add(item);
        Assert.Single(simplifiedOutputs);

        // Schapery edge branches: empty Unknown, single relaxation, and all Helmholtz fail
        Mock<ISchaperyModelCalculator> schaperyCalcMock = new();
        Mock<IFunctionCurveFitter> failingFnFitter = new();
        failingFnFitter.Setup(f => f.TryFit(It.IsAny<MathematicalCurveFitInput>()))
            .Returns((MathematicalCurveFitInput _) => ("mock", (CurveFitInput?)null, new InvalidOperationException("fail")));

        await using SchaperyRelaxationOnlyCurveFitterStep schaperySingle = new(
            NullLogger<SchaperyRelaxationOnlyCurveFitterStep>.Instance,
            schaperyCalcMock.Object,
            fitterMock.Object,
            failingFnFitter.Object,
            failingFnFitter.Object,
            failingFnFitter.Object,
            new FunctionFactory(),
            0.90);

        // Empty Unknown -> zero items
        List<MechanicalModelCurveFitOutput> emptyOut = [];
        await foreach (MechanicalModelCurveFitOutput item in schaperySingle.ExecuteAsync(new CurveSegment { Type = SegmentType.Unknown, TimePoints = [], ExperimentalStrain = [], ExperimentalStress = [] }))
            emptyOut.Add(item);
        Assert.Empty(emptyOut);

        // Single relaxation -> strain.Length == 1 branch returns ConstantFunction
        await foreach (MechanicalModelCurveFitOutput _ in schaperySingle.ExecuteAsync(relaxation)) { }
        List<MechanicalModelCurveFitOutput> singleOut = [];
        await foreach (MechanicalModelCurveFitOutput item in schaperySingle.ExecuteAsync(new CurveSegment { Type = SegmentType.Unknown, TimePoints = [], ExperimentalStrain = [], ExperimentalStress = [] }))
            singleOut.Add(item);
        Assert.Equal(2, singleOut.Count);

        // Multi-strain when all Helmholtz fitters fail -> throws InvalidOperationException
        await foreach (MechanicalModelCurveFitOutput _ in schaperySingle.ExecuteAsync(relaxation)) { }
        await foreach (MechanicalModelCurveFitOutput _ in schaperySingle.ExecuteAsync(relaxation with { ExperimentalStrain = [0.05, 0.05, 0.05] })) { }
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (MechanicalModelCurveFitOutput _ in schaperySingle.ExecuteAsync(new CurveSegment { Type = SegmentType.Unknown, TimePoints = [], ExperimentalStrain = [], ExperimentalStress = [] }))
            {
            }
        });
    }
}
