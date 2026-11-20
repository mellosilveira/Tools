namespace MelloSilveiraTools.WebUi.Models;

/// <summary>
/// Generic API result wrapper mirroring server <c>Result&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public sealed record ApiResultDto<T>
{
    /// <summary>Gets whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets actionable feedback or error messages returned by the server.</summary>
    public List<string> Messages { get; init; } = [];

    /// <summary>Gets the response payload when available.</summary>
    public T? Data { get; init; }

    /// <summary>Gets all messages joined into a single readable string.</summary>
    public string JoinedMessages => string.Join("; ", Messages);
}

/// <summary>
/// Generic paged API result wrapper mirroring server <c>PagedResult&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed record ApiPagedResultDto<T>
{
    /// <summary>Gets whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets actionable feedback or error messages returned by the server.</summary>
    public List<string> Messages { get; init; } = [];

    /// <summary>Gets the paged items returned by the server.</summary>
    public List<T> Data { get; init; } = [];

    /// <summary>Gets the total matching item count across all pages.</summary>
    public long TotalCount { get; init; }
}

/// <summary>
/// Response payload returned by JWE token generation and refresh endpoints.
/// </summary>
public sealed record AuthTokenDto
{
    /// <summary>Gets the signed and encrypted JWE token string.</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>Gets the UTC expiration timestamp of the token.</summary>
    public DateTimeOffset ExpiresOn { get; init; }
}

/// <summary>
/// Describes a pre-bundled knee ligament sample dataset available on the server.
/// </summary>
public sealed record SampleDatasetDto
{
    /// <summary>Gets the unique sample identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the human-readable dataset name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the specimen description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the resolved path to the raw strain CSV file.</summary>
    public string StrainFileLocation { get; init; } = string.Empty;

    /// <summary>Gets the resolved path to the raw stress CSV file.</summary>
    public string StressFileLocation { get; init; } = string.Empty;
}

/// <summary>
/// Response returned after uploading experimental strain and stress CSV files.
/// </summary>
public sealed record ExperimentalUploadResponseDto
{
    /// <summary>Gets the unique correlation identifier assigned to the upload.</summary>
    public string UploadIdentifier { get; init; } = string.Empty;

    /// <summary>Gets the canonical storage location of the strain CSV.</summary>
    public string StrainFileLocation { get; init; } = string.Empty;

    /// <summary>Gets the canonical storage location of the stress CSV.</summary>
    public string StressFileLocation { get; init; } = string.Empty;

    /// <summary>Gets the database metadata ID of the stored strain CSV.</summary>
    public long StrainStorageEntryId { get; init; }

    /// <summary>Gets the database metadata ID of the stored stress CSV.</summary>
    public long StressStorageEntryId { get; init; }
}

/// <summary>
/// Experimental group payload for pipeline execution.
/// </summary>
public sealed record ExperimentalDataGroupDto
{
    /// <summary>Gets the optional predefined analysis identifier.</summary>
    public string? Identifier { get; init; }

    /// <summary>Gets the optional initial time threshold to ignore transient data.</summary>
    public double? StartExperimentalTimeThreshold { get; init; }

    /// <summary>Gets the storage location of the raw strain CSV file.</summary>
    public required string StrainFileLocation { get; init; }

    /// <summary>Gets the storage location of the raw stress CSV file.</summary>
    public required string StressFileLocation { get; init; }
}

/// <summary>
/// Request payload mirroring <c>ExecuteExperimentalDataPipelineRequest</c>.
/// </summary>
public sealed record ExecuteExperimentalDataRequestDto
{
    /// <summary>Gets the experimental data group configuration.</summary>
    public required ExperimentalDataGroupDto ExperimentalDataGroupRequest { get; init; }

    /// <summary>Gets the target mechanical constitutive model name (e.g., <c>Fung</c>, <c>SimplifiedFung</c>, <c>Schapery</c>).</summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>Gets the target segment types to fit (e.g., <c>Relaxation</c>).</summary>
    public IReadOnlyList<string> TargetSegments { get; init; } = ["Relaxation"];

    /// <summary>Gets the curve fitter engine name (<c>Alglib</c> or <c>MathNet</c>).</summary>
    public string CurveFitterType { get; init; } = "Alglib";

    /// <summary>Gets the target R-squared determination coefficient threshold.</summary>
    public double TargetRSquared { get; init; } = 0.90;

    /// <summary>Gets the optional downsampling skip time step.</summary>
    public double? DownsamplingSkipTimeStep { get; init; }
}

/// <summary>
/// Real-time NDJSON progress notification mirroring <c>PipelineProgressEvent</c>.
/// </summary>
public sealed record PipelineProgressEventDto
{
    /// <summary>Gets the unique identifier of the pipeline execution emitting the event.</summary>
    public string PipelineIdentifier { get; init; } = string.Empty;

    /// <summary>Gets the logical name of the pipeline step emitting the event.</summary>
    public string StepName { get; init; } = string.Empty;

    /// <summary>Gets the human-readable description of the step activity.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the lifecycle state (<c>Pending</c>, <c>Started</c>, <c>Completed</c>, <c>Faulted</c>).</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>Gets the UTC timestamp when the progress event was generated.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Gets the immediate upstream step names feeding into this step.</summary>
    public IReadOnlyList<string> UpstreamSteps { get; init; } = [];

    /// <summary>Gets the cumulative count of items received by this step.</summary>
    public int ReceivedItems { get; init; }

    /// <summary>Gets the cumulative count of items processed by this step.</summary>
    public int ProcessedItems { get; init; }

    /// <summary>Gets optional serialized diagnostic details.</summary>
    public string? Details { get; init; }

    /// <summary>Gets the computed completion percentage for display.</summary>
    public double ProgressPercentage => ReceivedItems > 0
        ? Math.Min(100.0, (double)ProcessedItems / ReceivedItems * 100.0)
        : (State == "Completed" ? 100.0 : 0.0);
}

/// <summary>
/// Calibrated constitutive model fit output persisted in PostgreSQL.
/// </summary>
public sealed record MechanicalModelCurveFitOutputDto
{
    /// <summary>Gets the database primary key.</summary>
    public long Id { get; init; }

    /// <summary>Gets the deterministic curve fit identifier.</summary>
    public string Identifier { get; init; } = string.Empty;

    /// <summary>Gets the external pipeline run identifier.</summary>
    public string? ExtenalIdentifier { get; init; }

    /// <summary>Gets the mechanical constitutive model name.</summary>
    public string MechanicalModelName { get; init; } = string.Empty;

    /// <summary>Gets the optimization residual error.</summary>
    public double Error { get; init; }

    /// <summary>Gets the optimizer iteration count.</summary>
    public int Iterations { get; init; }

    /// <summary>Gets the serialized constitutive parameters JSON.</summary>
    public string ConstitutiveParameters { get; init; } = string.Empty;
}

/// <summary>
/// Forward mechanical simulation record persisted in PostgreSQL.
/// </summary>
public sealed record MechanicalModelSimulationDto
{
    /// <summary>Gets the database primary key.</summary>
    public long Id { get; init; }

    /// <summary>Gets the unique simulation identifier.</summary>
    public string Identifier { get; init; } = string.Empty;

    /// <summary>Gets the correlated curve fit identifier.</summary>
    public string CurveFitIdentifier { get; init; } = string.Empty;

    /// <summary>Gets the external pipeline run identifier.</summary>
    public string? ExternalIdentifier { get; init; }

    /// <summary>Gets the mechanical constitutive model name.</summary>
    public string MechanicalModelName { get; init; } = string.Empty;

    /// <summary>Gets the stored metadata ID of the segmented experimental CSV.</summary>
    public long ExperimentalDataFileMetadataId { get; init; }

    /// <summary>Gets the stored metadata ID of the simulated output CSV.</summary>
    public long SimulationOutputFileMetadataId { get; init; }

    /// <summary>Gets the detected relaxation asymptote time if reached.</summary>
    public double? AsymptoteTime { get; init; }
}

/// <summary>
/// Parametric sweep execution record persisted in PostgreSQL.
/// </summary>
public sealed record ParametricSweepDto
{
    /// <summary>Gets the database primary key.</summary>
    public long Id { get; init; }

    /// <summary>Gets the deterministic sweep configuration identifier.</summary>
    public string Identifier { get; init; } = string.Empty;

    /// <summary>Gets the external pipeline run identifier.</summary>
    public string? ExternalIdentifier { get; init; }

    /// <summary>Gets the mechanical constitutive model name.</summary>
    public string MechanicalModelName { get; init; } = string.Empty;

    /// <summary>Gets the baseline strain value.</summary>
    public double BaseStrain { get; init; }
}

/// <summary>
/// Stored artifact metadata record.
/// </summary>
public sealed record StorageEntryMetadataDto
{
    /// <summary>Gets the database primary key.</summary>
    public long Id { get; init; }

    /// <summary>Gets the external pipeline run identifier.</summary>
    public string? ExternalIdentifier { get; init; }

    /// <summary>Gets the storage domain context.</summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>Gets the stored file name.</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>Gets the storage path or URI.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>Gets the MIME content type.</summary>
    public string ContentType { get; init; } = string.Empty;
}

/// <summary>
/// Range parameters for parametric sweep variables.
/// </summary>
public sealed record RangeParametersDto
{
    /// <summary>Gets the sweep start value.</summary>
    public double InitialPoint { get; init; }

    /// <summary>Gets the sweep end value.</summary>
    public double FinalPoint { get; init; }

    /// <summary>Gets the discretization increment.</summary>
    public double Step { get; init; }
}

/// <summary>
/// Root-finding configuration for asymptotic evaluation.
/// </summary>
/// <param name="InitialGuess">Lower bound or initial time guess.</param>
/// <param name="FinalGuess">Upper bound or final time guess.</param>
/// <param name="Tolerance">Numerical convergence tolerance.</param>
/// <param name="MaxIterations">Maximum solver iterations.</param>
public sealed record RootFindingInputDto(double InitialGuess, double FinalGuess, double Tolerance, int MaxIterations);

/// <summary>
/// Request payload for <c>POST /api/v1/parametric-sweeps/process-from-experimental-data</c>.
/// </summary>
public sealed record ExecuteParametricSweepFromExperimentalDataRequestDto
{
    /// <summary>Gets the experimental pipeline identifier whose calibrated models will be swept.</summary>
    public required string ExperimentalDataIdentifier { get; init; }

    /// <summary>Gets the target mechanical constitutive model name.</summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>Gets the execution mode (<c>Sequential</c> or <c>Parallel</c>).</summary>
    public string ExecutionMode { get; init; } = "Sequential";

    /// <summary>Gets the baseline strain value.</summary>
    public double BaseStrain { get; init; } = 0.04;

    /// <summary>Gets the strain range definition.</summary>
    public required RangeParametersDto StrainRange { get; init; }

    /// <summary>Gets optional additional constitutive variable sweep ranges.</summary>
    public Dictionary<string, RangeParametersDto> ConstantVariables { get; init; } = [];

    /// <summary>Gets the ramp-time consideration regime.</summary>
    public string RampTimeConsideration { get; init; } = "Disregard";

    /// <summary>Gets the simulation initial time.</summary>
    public double InitialTime { get; init; } = 1.0;

    /// <summary>Gets the simulation time increment.</summary>
    public double TimeStep { get; init; } = 10.0;

    /// <summary>Gets the simulation final time.</summary>
    public double FinalTime { get; init; } = 51.0;

    /// <summary>Gets the root-finding algorithm name.</summary>
    public string RootFindingAlgorithm { get; init; } = "StepByStepMethod";

    /// <summary>Gets the base root-finding configuration.</summary>
    public required RootFindingInputDto BaseRootFindingInput { get; init; }
}

/// <summary>
/// Parsed stress-strain point used purely for UI chart rendering.
/// </summary>
/// <param name="Time">Elapsed time in seconds.</param>
/// <param name="Strain">Axial strain value.</param>
/// <param name="Stress">Axial stress value.</param>
/// <param name="SegmentType">Deformation phase code or name.</param>
public sealed record ChartDataPoint(double Time, double Strain, double Stress, string SegmentType);

/// <summary>
/// Parsed OAAT sensitivity row used purely for UI chart rendering.
/// </summary>
/// <param name="VariableValue">Swept variable value.</param>
/// <param name="InitialStress">Initial stress at $t_0$.</param>
/// <param name="FinalStress">Final stress at $t_f$.</param>
/// <param name="PercentageDeltaStress">Relative stress relaxation percentage.</param>
public sealed record SensitivityPoint(double VariableValue, double InitialStress, double FinalStress, double PercentageDeltaStress);
