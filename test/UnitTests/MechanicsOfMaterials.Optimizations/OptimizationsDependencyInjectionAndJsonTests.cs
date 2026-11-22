using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Single;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Steps.Generators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Serializers.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Polly;
using System.Text.Json;

namespace UnitTests.MechanicsOfMaterials.Optimizations;

/// <summary>
/// Unit tests for Optimizations DI registration, JSON options, range contracts, and persistence entities.
/// </summary>
public sealed class OptimizationsDependencyInjectionAndJsonTests
{
    private static readonly ResiliencePipelineSettings DefaultResilience = new()
    {
        BackoffType = DelayBackoffType.Constant,
        MaxRetryAttempts = 1,
        DelayInMilliseconds = 1,
        UseJitter = false
    };

    private static ExperimentalDataSettings CreateExperimentalDataSettings()
    {
        PipelineStepOptions stepOptions = new() { MaxBufferSize = 10 };
        return new ExperimentalDataSettings
        {
            SegmenterOptions = new() { MaxBufferSize = 10, MaxWorkers = 2, KeepOrder = false },
            FileWriterOptions = stepOptions,
            PersistanceOptions = stepOptions,
            SegmentBuilderOptions = new() { MaxBufferSize = 10, MaxWorkers = 1, KeepOrder = false },
            CurveFitterOptions = stepOptions,
            SimulationInputBuilderOptions = stepOptions,
            IngestionBufferCapacity = 10,
            MaxCsvSizeBytes = 50L * 1024 * 1024
        };
    }

    /// <summary>
    /// Verifies AddOptimizationsServices registers nothing when all settings are null,
    /// and registers the full graph of curve fitters, commands, validators, pipelines, and generators when provided.
    /// </summary>
    [Fact]
    public void AddOptimizationsServices_RegistersExpectedServicesPerConfiguration()
    {
        ServiceCollection emptyServices = new();
        emptyServices.AddOptimizationsServices();
        ServiceProvider emptyProvider = emptyServices.BuildServiceProvider();
        Assert.Null(emptyProvider.GetService<ICurveFitter>());
        Assert.Null(emptyProvider.GetService<IMechanicalModelStepFactory>());

        ServiceCollection services = new();
        services.AddLogging();
        services.AddCoreServices(DefaultResilience);
        services.AddMathematicsServices();
        services.AddMechanicsOfMaterialsServices(addMechanicalModels: true);
        services.AddSingleton(Mock.Of<IRepository>());
        services.AddSingleton(Mock.Of<IFileStorageService>());
        services.AddOptimizationsServices(
            curveFitterSettings: new CurveFitterSettings { DefaultCurveFitter = CurveFitterType.MathNet, Seed = 42 },
            experimentalDataSettings: CreateExperimentalDataSettings(),
            parametricSweepSettings: new ParametricSweepSettings { IngestionBufferCapacity = 16 });

        ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<MathNetCurveFitter>(provider.GetRequiredService<ICurveFitter>());
        Assert.IsType<AlglibCurveFitter>(provider.GetRequiredKeyedService<ICurveFitter>(CurveFitterType.Alglib));
        Assert.IsType<MathNetCurveFitter>(provider.GetRequiredKeyedService<ICurveFitter>(CurveFitterType.MathNet));
        Assert.IsType<MechanicalModelStepFactory>(provider.GetRequiredService<IMechanicalModelStepFactory>());
        Assert.IsType<PronySeriesCurveFitter>(provider.GetRequiredKeyedService<IMathExpressionCurveFitter>(MathExpressionType.PronySeries));
        Assert.IsType<LogarithmicCurveFitter>(provider.GetRequiredKeyedService<IFunctionCurveFitter>(FunctionType.Logarithmic));
        Assert.IsType<ExponentialCurveFitter>(provider.GetRequiredKeyedService<IFunctionCurveFitter>(FunctionType.Exponential));
        Assert.IsType<PolynomialCurveFitter>(provider.GetRequiredKeyedService<IFunctionCurveFitter>(FunctionType.Polynomial));

        Assert.NotNull(provider.GetRequiredService<ExecuteExperimentalDataPipeline>());
        Assert.NotNull(provider.GetRequiredService<ExecuteBatchExperimentalDataPipeline>());
        Assert.IsType<ExperimentalDataProcessingPipeline>(provider.GetRequiredService<IPipeline<ExperimentalDataProcessingInput, string>>());

        Assert.NotNull(provider.GetRequiredService<ExecuteParametricSweepPipeline>());
        Assert.NotNull(provider.GetRequiredService<ExecuteParametricSweepFromExperimentalDataCommand>());
        Assert.IsType<ParametricSweepPipeline>(provider.GetRequiredService<IPipeline<ParametricSweepPipelineInput, string>>());
        Assert.IsType<FungParametricSweepGeneratorStep>(provider.GetRequiredKeyedService<IParametricSweepGeneratorStep>(nameof(MechanicalModel.Fung)));
        Assert.IsType<SimplifiedFungParametricSweepGeneratorStep>(provider.GetRequiredKeyedService<IParametricSweepGeneratorStep>(nameof(MechanicalModel.SimplifiedFung)));
        Assert.IsType<SchaperyParametricSweepGeneratorStep>(provider.GetRequiredKeyedService<IParametricSweepGeneratorStep>(nameof(MechanicalModel.Schapery)));
    }

    /// <summary>
    /// Verifies OptimizationJsonOptions Serialize and Deserialize helpers and Range/Entity DTO construction.
    /// </summary>
    [Fact]
    public void OptimizationJsonOptionsAndModels_RoundTripCorrectly()
    {
        RangeParameters range = new() { InitialPoint = 1.23456789, Step = 0.5, FinalPoint = 2.5 };
        string json = OptimizationJsonOptions.Serialize(range);

        RangeParameters? typed = OptimizationJsonOptions.Deserialize<RangeParameters>(json);
        object? untyped = OptimizationJsonOptions.Deserialize(json, typeof(RangeParameters));

        Assert.NotNull(typed);
        Assert.Equal(1.234568, typed.InitialPoint);
        Assert.IsType<RangeParameters>(untyped);

        RangeFunction rangeFunc = new(0.0, 1.0, [FunctionType.Polynomial], [range]);
        RangePowerLaw rangePow = new(0.0, 1.0, [range]);
        RangePronySeries rangeProny = new(0.0, 1.0, range, [range]);
        RangeReducedRelaxationFunction rangeRrf = new(range, range, range);
        Assert.Single(rangeFunc.Types);
        Assert.Single(rangePow.Coefficients);
        Assert.Single(rangeProny.Coefficients);
        Assert.Equal(range, rangeRrf.RelaxationStiffness);

        MechanicalModelCurveFitOutputEntity emptyEntity = new();
        Assert.Equal(string.Empty, emptyEntity.Identifier);

        MechanicalModelCurveFitOutputFilter filter = new() { ExtenalIdentifier = "ext-1", MechanicalModelName = "Fung" };
        Assert.Equal("ext-1", filter.ExtenalIdentifier);
        Assert.Equal("Fung", filter.MechanicalModelName);

        Assert.Equal(0.0, rangeProny.InitialVariableValue);
        Assert.Equal(1.0, rangeProny.FinalVariableValue);
        Assert.Equal(range, rangeProny.IndependentParameter);

        MechanicalModelOutput mOut = new() { Stress = 1.0, Strain = 0.1 };
        MechanicalModelSimulationOutput simOut = new("sim-1", "fit-1", "out.csv", mOut, mOut, mOut, mOut, 5.0);
        Assert.Equal("sim-1", simOut.Identifier);
        Assert.Equal("fit-1", simOut.CurveFitIdentifier);
        Assert.Equal("out.csv", simOut.OutputFullFileName);
        Assert.Same(mOut, simOut.InitialOutput);
        Assert.Same(mOut, simOut.FinalOutput);
        Assert.Same(mOut, simOut.AbsoluteDeltaOutput);
        Assert.Same(mOut, simOut.PercentageDeltaOutput);
        Assert.Equal(5.0, simOut.AsymptoteTime);

        ParametricSweepExecutionResult execRes = new("fit-1", "Fung", "sweep-1", true, "none");
        Assert.Equal("fit-1", execRes.CurveFitOutputIdentifier);
        Assert.Equal("Fung", execRes.MechanicalModelName);
        Assert.Equal("sweep-1", execRes.SweepIdentifier);
        Assert.True(execRes.Success);
        Assert.Equal("none", execRes.ErrorMessage);

        SimulationMetrics metrics = new(10.0, 5.0, 12.0, 4.9);
        Assert.Equal(10.0, metrics.InitialStress);
        Assert.Equal(5.0, metrics.FinalStress);
        Assert.Equal(12.0, metrics.AsymptoteTime);
        Assert.Equal(4.9, metrics.AsymptoteStress);

        ParametricSweepOutput sweepOut = new("c", 1.5, 10.0, 5.0, 12.0, 0.01, 4.9, -5.0, -50.0);
        Assert.Equal("c", sweepOut.VariableName);
        Assert.Equal(0.01, sweepOut.AsymptoteError);

        Assert.True(double.IsNaN(OptimizationJsonOptions.Deserialize<double>("\"NaN\"")));
        Assert.True(double.IsPositiveInfinity(OptimizationJsonOptions.Deserialize<double>("\"Infinity\"")));
        Assert.True(double.IsNegativeInfinity(OptimizationJsonOptions.Deserialize<double>("\"-Infinity\"")));
        Assert.Equal(42.5, OptimizationJsonOptions.Deserialize<double>("\"42.5\""));
        Assert.Throws<JsonException>(() => OptimizationJsonOptions.Deserialize<double>("\"invalid-number\""));
        Assert.Equal("\"NaN\"", OptimizationJsonOptions.Serialize(double.NaN));
        Assert.Equal("\"Infinity\"", OptimizationJsonOptions.Serialize(double.PositiveInfinity));
        Assert.Equal("\"-Infinity\"", OptimizationJsonOptions.Serialize(double.NegativeInfinity));
    }

    /// <summary>
    /// Verifies ExperimentalDataProcessingPipeline and ParametricSweepPipeline execute their full Dataflow topologies via DI.
    /// </summary>
    [Fact]
    public async Task Pipelines_ProcessAsync_ExecutesFullDataflowTopologies()
    {
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteTimebasedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ((Stream)new MemoryStream(), "loc.csv"));
        storageMock
            .Setup(s => s.CreateWriteTimebasedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ((Stream)new MemoryStream(), "loc.csv"));
        storageMock
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ((Stream)new MemoryStream(), "loc.csv"));
        storageMock
            .Setup(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => (Stream)new MemoryStream("Time,Value\n"u8.ToArray()));
        storageMock
            .Setup(s => s.GetMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MelloSilveiraTools.Core.Files.Models.StorageEntryMetadata(
                MelloSilveiraTools.Core.Files.Models.StorageProvider.Local,
                "loc.csv",
                "dir",
                "loc.csv",
                16,
                DateTimeOffset.UtcNow));

        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.TryInsertAsync(It.IsAny<MelloSilveiraTools.Database.RelationalDatabase.Models.Entities.StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MelloSilveiraTools.Core.Models.Result.CreateSuccessOk(1L));
        repoMock
            .Setup(r => r.TryInsertAsync(It.IsAny<MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities.ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MelloSilveiraTools.Core.Models.Result.CreateSuccessOk(1L));

        ServiceCollection services = new();
        services.AddLogging();
        services.AddCoreServices(DefaultResilience);
        services.AddMathematicsServices();
        services.AddMechanicsOfMaterialsServices(addMechanicalModels: true);
        services.AddSingleton(repoMock.Object);
        services.AddSingleton(storageMock.Object);
        services.AddOptimizationsServices(
            curveFitterSettings: new CurveFitterSettings { DefaultCurveFitter = CurveFitterType.Alglib },
            experimentalDataSettings: CreateExperimentalDataSettings(),
            parametricSweepSettings: new ParametricSweepSettings { IngestionBufferCapacity = 16 });

        await using ServiceProvider provider = services.BuildServiceProvider();

        IPipeline<ExperimentalDataProcessingInput, string> expPipeline = provider.GetRequiredService<IPipeline<ExperimentalDataProcessingInput, string>>();
        ExperimentalDataProcessingInput expInput = new()
        {
            Identifier = "exp-pipe-test",
            StrainFileLocation = "strain.csv",
            StressFileLocation = "stress.csv",
            MechanicalModelName = nameof(MechanicalModel.Fung),
            TargetSegments = [SegmentType.Relaxation],
            CurveFitterType = CurveFitterType.Alglib,
            TargetRSquared = 0.9,
            ConfidenceLevel = 0.95,
            Options = new ExperimentalDataProcessingOptions()
        };
        _ = expInput.ToSegmenterInput();
        MelloSilveiraTools.Core.Models.Result<string> expResult = await expPipeline.ProcessAsync(expInput);
        Assert.True(expResult.Success);
        Assert.Equal("exp-pipe-test", expResult.Data);

        IPipeline<ParametricSweepPipelineInput, string> sweepPipeline = provider.GetRequiredService<IPipeline<ParametricSweepPipelineInput, string>>();
        ParametricSweepPipelineInput sweepInput = new()
        {
            Identifier = "sweep-pipe-test",
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Baseline = new MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear.FungConstitutiveParameters
            {
                ElasticStressConstant = 1.0,
                ElasticPowerConstant = 1.0,
                ReducedRelaxationFunction = new MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear.ReducedRelaxationFunction(0.5, 1.0, 10.0)
            },
            ConstantVariables = [],
            ArrayVariables = [],
            BaseStrain = 0.05,
            StrainRange = new RangeParameters { InitialPoint = 0.05 },
            RampTimeConsideration = MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 2.0,
            TimeStep = 1.0,
            RootFindingAlgorithm = MelloSilveiraTools.Mathematics.Models.NumericalMethods.RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms.RootFindingInput(0.0, 2.0, 1e-3, 10)
        };
        MelloSilveiraTools.Core.Models.Result<string> sweepResult = await sweepPipeline.ProcessAsync(sweepInput);
        Assert.True(sweepResult.Success);
        Assert.Equal("sweep-pipe-test", sweepResult.Data);

        // Also exercise Ramp + Relaxation combinations on Fung and SimplifiedFung curve fitter steps with fast mocks
        Mock<ICurveFitter> fastFitterMock = new();
        fastFitterMock
            .Setup(f => f.Fit(It.IsAny<CurveFitInput>()))
            .Returns((CurveFitInput inp) =>
            {
                _ = inp.ValidateParameters?.Invoke(inp.InitialParameters);
                double[] xSample = [.. inp.IndependentVariables.Select(v => v[0])];
                _ = inp.Calculate(inp.InitialParameters, xSample);
                List<ParameterConfidenceInterval> cis = [.. inp.InitialParameters.Select((v, idx) => new ParameterConfidenceInterval(idx, v, v, v, 0.0, 0.95))];
                return new CurveFitOutput(cis, 0.001, 0.99, 5);
            });

        Mock<IMathExpressionCurveFitter> fastPronyMock = new();
        fastPronyMock
            .Setup(f => f.Fit(It.IsAny<MathematicalCurveFitInput>()))
            .Returns((MathematicalCurveFitInput inp) =>
            {
                _ = inp.ValidateParameters?.Invoke(inp.InitialParameters!);
                _ = inp.ValidateParameters?.Invoke([0.5, 0.2, -0.5, 0.15, -0.1, 0.15, -0.01]);
                List<ParameterConfidenceInterval> cis = [.. inp.InitialParameters!.Select((v, idx) => new ParameterConfidenceInterval(idx, v, v, v, 0.0, 0.95))];
                return new CurveFitOutput(cis, 0.001, 0.99, 5);
            });

        CurveSegment rampSeg = new()
        {
            Type = SegmentType.Ramp,
            TimePoints = [0.0, 1.0],
            ExperimentalStrain = [0.0, 0.05],
            ExperimentalStress = [0.0, 10.0]
        };
        CurveSegment zeroSpanRampSeg = new()
        {
            Type = SegmentType.Ramp,
            TimePoints = [1.0],
            ExperimentalStrain = [0.05],
            ExperimentalStress = [10.0]
        };
        CurveSegment relaxSeg = new()
        {
            Type = SegmentType.Relaxation,
            TimePoints = [1.0, 2.0, 5.0, 10.0],
            ExperimentalStrain = [0.05, 0.05, 0.05, 0.05],
            ExperimentalStress = [10.0, 8.0, 7.0, 6.5]
        };

        MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter.Fung.FungRelaxationOnlyCurveFitterStep fungStep = new(
            provider.GetRequiredService<MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung.IFungModelCalculator>(),
            fastFitterMock.Object,
            0.9,
            0.95);

        await foreach (MechanicalModelCurveFitOutput _ in fungStep.ExecuteAsync(rampSeg)) { }
        List<MechanicalModelCurveFitOutput> fungRampOutputs = [];
        await foreach (MechanicalModelCurveFitOutput item in fungStep.ExecuteAsync(relaxSeg))
            fungRampOutputs.Add(item);
        Assert.Equal(2, fungRampOutputs.Count);

        MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter.SimplifiedFungRelaxationOnlyCurveFitterStep simpFungStep = new(
            provider.GetRequiredService<MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung.ISimplifiedFungModelCalculator>(),
            fastFitterMock.Object,
            fastPronyMock.Object,
            0.9,
            0.95);

        await foreach (MechanicalModelCurveFitOutput _ in simpFungStep.ExecuteAsync(zeroSpanRampSeg)) { }
        List<MechanicalModelCurveFitOutput> simpFungRampOutputs = [];
        await foreach (MechanicalModelCurveFitOutput item in simpFungStep.ExecuteAsync(relaxSeg))
            simpFungRampOutputs.Add(item);
        Assert.Equal(2, simpFungRampOutputs.Count);
    }
}
