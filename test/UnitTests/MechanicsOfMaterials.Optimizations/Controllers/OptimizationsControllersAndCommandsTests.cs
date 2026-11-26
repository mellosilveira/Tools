using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Single;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Controllers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Controllers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Unit tests for Optimizations controllers (<see cref="ExperimentalDataController"/>,
/// <see cref="ParametricSweepController"/>, <see cref="PipelineProgressController"/>) and experimental commands.
/// </summary>
public sealed class OptimizationsControllersAndCommandsTests
{
    /// <summary>
    /// Verifies ExecuteExperimentalDataPipeline and ExecuteBatchExperimentalDataPipeline mapping,
    /// sequential/parallel execution, and exception isolation.
    /// </summary>
    [Fact]
    public async Task ExperimentalDataCommands_ExecuteSingleAndBatchWithIsolation()
    {
        ExperimentalDataProcessingInput? singleCaptured = null;
        TaskCompletionSource singleDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IPipeline<ExperimentalDataProcessingInput, string>> pipeMock = new();
        pipeMock
            .Setup(p => p.ProcessAsync(It.IsAny<ExperimentalDataProcessingInput>(), It.IsAny<CancellationToken>()))
            .Callback<ExperimentalDataProcessingInput, CancellationToken>((inp, _) =>
            {
                singleCaptured = inp;
                singleDone.TrySetResult();
            })
            .ReturnsAsync(Result.CreateSuccessOk("pipe-done"));

        ExecuteExperimentalDataPipeline singleCmd = new(
            NullLogger<ExecuteExperimentalDataPipeline>.Instance,
            pipeMock.Object,
            new ExecuteExperimentalDataPipelineValidator());

        ExecuteExperimentalDataPipelineRequest singleReq = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Fung),
            ExperimentalDataGroupRequest = new ExperimentalDataGroupRequest
            {
                Identifier = " ",
                StrainFileLocation = "strain.csv",
                StressFileLocation = "stress.csv",
                StartExperimentalTimeThreshold = 0.5
            }
        };

        Result<string> singleRes = await singleCmd.ExecuteAsync(singleReq);
        Assert.True(singleRes.Success);
        await singleDone.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(singleCaptured);
        Assert.False(string.IsNullOrWhiteSpace(singleCaptured.Identifier));

        // Batch: first group succeeds, second throws (isolated)
        int callCount = 0;
        TaskCompletionSource allDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
        pipeMock
            .Setup(p => p.ProcessAsync(It.IsAny<ExperimentalDataProcessingInput>(), It.IsAny<CancellationToken>()))
            .Returns((ExperimentalDataProcessingInput _, CancellationToken _) =>
            {
                int current = Interlocked.Increment(ref callCount);
                if (current == 2)
                {
                    allDone.TrySetResult();
                    throw new InvalidOperationException("boom");
                }

                return Task.FromResult(Result.CreateSuccessOk("ok"));
            });

        ExecuteBatchExperimentalDataPipeline batchCmd = new(
            NullLogger<ExecuteBatchExperimentalDataPipeline>.Instance,
            pipeMock.Object,
            new ExecuteBatchExperimentalDataPipelineValidator());

        ExecuteBatchExperimentalDataPipelineRequest batchReq = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Fung),
            ExecutionMode = PipelineExecutionMode.Parallel,
            MaxDegreeOfParallelism = 2,
            Groups =
            [
                new ExperimentalDataGroupRequest { Identifier = "g1", StrainFileLocation = "s1.csv", StressFileLocation = "st1.csv" },
                new ExperimentalDataGroupRequest { Identifier = " ", StrainFileLocation = "s2.csv", StressFileLocation = "st2.csv" }
            ]
        };

        Result<IAsyncEnumerable<string>> batchRes = await batchCmd.ExecuteAsync(batchReq);
        Assert.True(batchRes.Success);
        List<string> ids = [];
        await foreach (string id in batchRes.Data!)
            ids.Add(id);

        Assert.Equal(2, ids.Count);
        Assert.Equal("g1", ids[0]);
        await allDone.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);
    }

    /// <summary>
    /// Verifies ExperimentalDataController Process and ProcessBatch (both invalid and valid NDJSON response).
    /// </summary>
    [Fact]
    public async Task ExperimentalDataController_ProcessAndProcessBatch_WorkAsExpected()
    {
        ExperimentalDataController controller = new(NullLogger<ExperimentalDataController>.Instance);
        DefaultHttpContext httpContext = new();
        MemoryStream responseBody = new();
        httpContext.Response.Body = responseBody;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        Mock<IPipeline<ExperimentalDataProcessingInput, string>> pipeMock = new();
        pipeMock.Setup(p => p.ProcessAsync(It.IsAny<ExperimentalDataProcessingInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk("ok"));

        ExecuteExperimentalDataPipeline singleCmd = new(
            NullLogger<ExecuteExperimentalDataPipeline>.Instance,
            pipeMock.Object,
            new ExecuteExperimentalDataPipelineValidator());

        IActionResult singleRes = await controller.Process(singleCmd, new ExecuteExperimentalDataPipelineRequest
        {
            MechanicalModelName = nameof(MechanicalModel.Fung),
            ExperimentalDataGroupRequest = new ExperimentalDataGroupRequest
            {
                Identifier = "single-1",
                StrainFileLocation = "s.csv",
                StressFileLocation = "st.csv"
            }
        });
        Assert.IsType<JsonResult>(singleRes);

        ExecuteBatchExperimentalDataPipeline batchCmd = new(
            NullLogger<ExecuteBatchExperimentalDataPipeline>.Instance,
            pipeMock.Object,
            new ExecuteBatchExperimentalDataPipelineValidator());

        // Invalid request -> 400
        IActionResult badBatch = await controller.ProcessBatch(batchCmd, new ExecuteBatchExperimentalDataPipelineRequest
        {
            MechanicalModelName = "",
            Groups = []
        });
        JsonResult badJson = Assert.IsType<JsonResult>(badBatch);
        Assert.Equal(400, badJson.StatusCode);

        // Valid request -> NDJSON write
        TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        pipeMock.Setup(p => p.ProcessAsync(It.IsAny<ExperimentalDataProcessingInput>(), It.IsAny<CancellationToken>()))
            .Callback(() => finished.TrySetResult())
            .ReturnsAsync(Result.CreateSuccessOk("ok"));

        IActionResult okBatch = await controller.ProcessBatch(batchCmd, new ExecuteBatchExperimentalDataPipelineRequest
        {
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Groups = [new ExperimentalDataGroupRequest { Identifier = "batch-1", StrainFileLocation = "s.csv", StressFileLocation = "st.csv" }]
        });
        Assert.IsType<EmptyResult>(okBatch);
        Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
        Assert.Contains("batch-1", Encoding.UTF8.GetString(responseBody.ToArray()));
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);
    }

    /// <summary>
    /// Verifies ParametricSweepController Process and ProcessFromExperimentalData (invalid &amp; valid NDJSON).
    /// </summary>
    [Fact]
    public async Task ParametricSweepController_ProcessAndProcessFromExperimentalData_WorkAsExpected()
    {
        ParametricSweepController controller = new(NullLogger<ParametricSweepController>.Instance);
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        Mock<IPipeline<ParametricSweepPipelineInput, string>> pipeMock = new();
        pipeMock.Setup(p => p.ProcessAsync(It.IsAny<ParametricSweepPipelineInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk("ok"));

        ExecuteParametricSweepPipeline singleCmd = new(
            NullLogger<ExecuteParametricSweepPipeline>.Instance,
            pipeMock.Object,
            new ExecuteParametricSweepValidator());

        IActionResult processRes = await controller.Process(singleCmd, new ExecuteParametricSweepPipelineRequest
        {
            Identifier = "sw-1",
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Baseline = new FungConstitutiveParameters(10.0, 2.0, new ReducedRelaxationFunction(0.5, 1.0, 10.0)),
            StrainRange = new RangeParameters { InitialPoint = 0.03 },
            BaseStrain = 0.03,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            TimeStep = 0.5,
            FinalTime = 5.0,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(0.0, 5.0, 1e-6, 20)
        });
        Assert.IsType<JsonResult>(processRes);

        ServiceProvider sp = new ServiceCollection().BuildServiceProvider();
        ExecuteParametricSweepFromExperimentalDataCommand fromExpCmd = new(
            NullLogger<ExecuteParametricSweepFromExperimentalDataCommand>.Instance,
            Mock.Of<IRepository>(),
            pipeMock.Object,
            new ExecuteParametricSweepFromExperimentalDataValidator(),
            new ServiceLocator(new InMemoryDynamicServiceProvider(sp), sp));

        // Invalid request -> 400
        IActionResult badFromExp = await controller.ProcessFromExperimentalData(fromExpCmd, new ExecuteParametricSweepFromExperimentalDataRequest
        {
            ExperimentalDataIdentifier = "",
            MechanicalModelName = nameof(MechanicalModel.Fung),
            BaseStrain = 0.03,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            TimeStep = 0.0,
            FinalTime = 0.0,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(0.0, 5.0, 1e-6, 20)
        });
        JsonResult badResult = Assert.IsType<JsonResult>(badFromExp);
        Assert.Equal(400, badResult.StatusCode);

        // Valid request -> 200 NDJSON stream
        Mock<IRepository> okRepoMock = new();
        okRepoMock.Setup(r => r.GetAsync<
                MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities.MechanicalModelCurveFitOutputEntity,
                MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities.MechanicalModelCurveFitOutputFilter>(
                It.IsAny<MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities.MechanicalModelCurveFitOutputFilter>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(OneFungEntityAsync());

        ServiceCollection okServices = new();
        okServices.AddKeyedSingleton<MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers.IMechanicalModelTypeResolver, MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers.FungModelTypeResolver>(nameof(MechanicalModel.Fung));
        ServiceProvider okSp = okServices.BuildServiceProvider();
        ExecuteParametricSweepFromExperimentalDataCommand okFromExpCmd = new(
            NullLogger<ExecuteParametricSweepFromExperimentalDataCommand>.Instance,
            okRepoMock.Object,
            pipeMock.Object,
            new ExecuteParametricSweepFromExperimentalDataValidator(),
            new ServiceLocator(new InMemoryDynamicServiceProvider(okSp), okSp));

        DefaultHttpContext okHttpContext = new();
        await using MemoryStream okBody = new();
        okHttpContext.Response.Body = okBody;
        controller.ControllerContext = new ControllerContext { HttpContext = okHttpContext };

        TaskCompletionSource sweepDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
        pipeMock.Setup(p => p.ProcessAsync(It.IsAny<ParametricSweepPipelineInput>(), It.IsAny<CancellationToken>()))
            .Callback(() => sweepDone.TrySetResult())
            .ReturnsAsync(Result.CreateSuccessOk("ok"));

        IActionResult okFromExp = await controller.ProcessFromExperimentalData(okFromExpCmd, new ExecuteParametricSweepFromExperimentalDataRequest
        {
            ExperimentalDataIdentifier = "exp-ok",
            MechanicalModelName = nameof(MechanicalModel.Fung),
            BaseStrain = 0.03,
            StrainRange = new RangeParameters { InitialPoint = 0.02, Step = 0.01, FinalPoint = 0.03 },
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            TimeStep = 0.5,
            FinalTime = 5.0,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(0.0, 5.0, 1e-6, 20)
        });
        Assert.IsType<EmptyResult>(okFromExp);
        Assert.Equal(StatusCodes.Status202Accepted, okHttpContext.Response.StatusCode);
        await sweepDone.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(30);

        static async IAsyncEnumerable<MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities.MechanicalModelCurveFitOutputEntity> OneFungEntityAsync()
        {
            await Task.CompletedTask;
            yield return new MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities.MechanicalModelCurveFitOutputEntity
            {
                Identifier = "fit-1",
                ExtenalIdentifier = "exp-ok",
                MechanicalModelName = nameof(MechanicalModel.Fung),
                InitialAcceptedRange = 0.0,
                FinalAcceptedRange = 0.05,
                LoadResponseRelationship = LoadResponseRelationship.StressStrain,
                RampTimeConsideration = RampTimeConsideration.Disregard,
                ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                Error = 0.01,
                Iterations = 1,
                ConstitutiveParameters = "{\"ElasticStressConstant\":10.0,\"ElasticPowerConstant\":2.0,\"ReducedRelaxationFunction\":{\"RelaxationStiffness\":0.5,\"FastRelaxationTime\":1.0,\"SlowRelaxationTime\":10.0}}"
            };
        }
    }

    /// <summary>
    /// Verifies PipelineProgressController streams NDJSON PipelineProgressEvent items to the HTTP response body.
    /// </summary>
    [Fact]
    public async Task PipelineProgressController_StreamProgressAsync_WritesNdjsonEvents()
    {
        PipelineProgressController controller = new(NullLogger<PipelineProgressController>.Instance);
        DefaultHttpContext httpContext = new();
        await using MemoryStream body = new();
        httpContext.Response.Body = body;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        Mock<IPipelineProgressNotifier> notifierMock = new();
        notifierMock
            .Setup(n => n.ReadAllAsync("pipe-42", It.IsAny<CancellationToken>()))
            .Returns(EventsAsync());

        await controller.StreamProgressAsync(notifierMock.Object, "pipe-42");

        string ndjson = Encoding.UTF8.GetString(body.ToArray());
        Assert.Contains("Segmenter", ndjson);
        Assert.Contains($"\"State\":{(int)PipelineStepState.Completed}", ndjson);

        static async IAsyncEnumerable<PipelineProgressEvent> EventsAsync()
        {
            await Task.CompletedTask;
            yield return new PipelineProgressEvent
            {
                PipelineIdentifier = "pipe-42",
                StepName = "Segmenter",
                Description = "Segmenting",
                State = PipelineStepState.Completed,
                Timestamp = DateTimeOffset.UtcNow,
                UpstreamSteps = [],
                ReceivedItems = 10,
                ProcessedItems = 10,
                Details = "done"
            };
        }
    }

    /// <summary>
    /// Verifies all validation branches in ExecuteExperimentalDataPipelineValidator, ExecuteBatchExperimentalDataPipelineValidator,
    /// ExecuteParametricSweepPipelineValidator, and ExecuteParametricSweepFromExperimentalDataValidator.
    /// </summary>
    [Fact]
    public async Task Validators_CoverAllCommonAndRangeBranches()
    {
        ExecuteExperimentalDataPipelineValidator singleExpValidator = new();
        Result badSingleExp = await singleExpValidator.ValidateAsync(new ExecuteExperimentalDataPipelineRequest
        {
            ExperimentalDataGroupRequest = null!,
            MechanicalModelName = "",
            CurveFitterType = (CurveFitterType)999,
            TargetRSquared = double.NaN,
            ConfidenceLevel = 1.0,
            AsymptoteConsecutivePointsThreshold = -1,
            SimulationTimeStep = 0.0,
            FinalSimulationTime = -1.0,
            DownsamplingSkipTimeStep = -0.1,
            TargetSegments = [(SegmentType)999]
        });
        Assert.False(badSingleExp.Success);

        Result badSingleExpNanBounds = await singleExpValidator.ValidateAsync(new ExecuteExperimentalDataPipelineRequest
        {
            ExperimentalDataGroupRequest = new ExperimentalDataGroupRequest
            {
                StrainFileLocation = "",
                StressFileLocation = ""
            },
            MechanicalModelName = "Fung",
            CurveFitterType = CurveFitterType.Alglib,
            TargetRSquared = 1.5,
            ConfidenceLevel = double.NaN,
            SimulationTimeStep = double.NaN,
            FinalSimulationTime = double.NaN,
            DownsamplingSkipTimeStep = double.NaN,
            TargetSegments = [SegmentType.Ramp]
        });
        Assert.False(badSingleExpNanBounds.Success);

        ExecuteParametricSweepValidator sweepValidator = new();
        Result badSweep1 = await sweepValidator.ValidateAsync(new ExecuteParametricSweepPipelineRequest
        {
            MechanicalModelName = "",
            Baseline = null!,
            BaseStrain = double.NaN,
            RampTimeConsideration = (RampTimeConsideration)999,
            RampTime = 0.0,
            InitialTime = double.NaN,
            FinalTime = double.NaN,
            TimeStep = 0.0,
            RootFindingAlgorithm = (RootFindingAlgorithm)999,
            BaseRootFindingInput = null!,
            StrainRange = new RangeParameters { InitialPoint = double.NaN, FinalPoint = double.NaN },
            ConstantVariables = new Dictionary<string, RangeParameters>
            {
                ["nullRange"] = null!,
                ["singlePoint"] = new RangeParameters { InitialPoint = 1.0 },
                ["inverted"] = new RangeParameters { InitialPoint = 5.0, FinalPoint = 1.0, Step = 0.0 },
                ["multBad"] = new RangeParameters { InitialPoint = 0.0, FinalPoint = 2.0, MultiplicativeFactor = 1.0 }
            },
            ArrayVariables = new Dictionary<string, Dictionary<int, RangeParameters>>
            {
                ["nullDict"] = null!,
                ["withNullElem"] = new Dictionary<int, RangeParameters>
                {
                    [0] = null!,
                    [1] = new RangeParameters { InitialPoint = 1.0, FinalPoint = 2.0, Step = double.NaN },
                    [2] = new RangeParameters { InitialPoint = 1.0, FinalPoint = 2.0, MultiplicativeFactor = double.NaN }
                }
            }
        });
        Assert.False(badSweep1.Success);

        Result badSweep2 = await sweepValidator.ValidateAsync(new ExecuteParametricSweepPipelineRequest
        {
            MechanicalModelName = "Fung",
            Baseline = new FungConstitutiveParameters(1.0, 1.0, new ReducedRelaxationFunction(0.5, 1.0, 10.0)),
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.ConsiderWithViscoelasticEffect,
            RampTime = double.NaN,
            InitialTime = 5.0,
            FinalTime = 2.0,
            TimeStep = double.NaN,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(double.NaN, double.NaN, double.NaN, 0)
        });
        Assert.False(badSweep2.Success);

        Result badSweep3 = await sweepValidator.ValidateAsync(new ExecuteParametricSweepPipelineRequest
        {
            MechanicalModelName = "Fung",
            Baseline = new FungConstitutiveParameters(1.0, 1.0, new ReducedRelaxationFunction(0.5, 1.0, 10.0)),
            BaseStrain = 0.05,
            ConstantVariables = [],
            ArrayVariables = [],
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 5.0,
            TimeStep = 0.5,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(5.0, 1.0, 0.0, 10)
        });
        Assert.False(badSweep3.Success);

        ExecuteBatchExperimentalDataPipelineValidator batchExpValidator = new();
        Result nullSingle = await singleExpValidator.ValidateAsync(null!);
        Result nullBatch = await batchExpValidator.ValidateAsync(null!);
        Assert.False(nullSingle.Success);
        Assert.False(nullBatch.Success);
        Result badBatchExp = await batchExpValidator.ValidateAsync(new ExecuteBatchExperimentalDataPipelineRequest
        {
            Groups = [],
            MechanicalModelName = "Fung",
            ExecutionMode = PipelineExecutionMode.Parallel,
            MaxDegreeOfParallelism = 0
        });
        Assert.False(badBatchExp.Success);

        Result badBatchExpNullGroups = await batchExpValidator.ValidateAsync(new ExecuteBatchExperimentalDataPipelineRequest
        {
            Groups = null!,
            MechanicalModelName = "Fung",
            ExecutionMode = PipelineExecutionMode.Parallel,
            MaxDegreeOfParallelism = 1
        });
        Assert.False(badBatchExpNullGroups.Success);

        Result badBatchExpGroups = await batchExpValidator.ValidateAsync(new ExecuteBatchExperimentalDataPipelineRequest
        {
            Groups =
            [
                null!,
                new ExperimentalDataGroupRequest { StrainFileLocation = "", StressFileLocation = "" }
            ],
            MechanicalModelName = "Fung",
            ExecutionMode = (PipelineExecutionMode)999,
            MaxDegreeOfParallelism = 1
        });
        Assert.False(badBatchExpGroups.Success);
    }
}
