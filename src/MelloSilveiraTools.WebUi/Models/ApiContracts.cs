using System.Text.Json;
using System.Text.Json.Serialization;

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
/// Response payload returned by JWE token generation and refresh endpoints, mirroring server <c>AuthenticationToken</c>.
/// </summary>
public sealed record AuthTokenDto
{
    /// <summary>Gets the signed and encrypted JWE token string.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>Gets the UTC expiration timestamp of the token.</summary>
    public DateTimeOffset ExpiresOn { get; init; }
}

/// <summary>
/// Public identity and privilege summary returned by <c>GET /api/v1/auth/me</c>.
/// </summary>
public sealed record CurrentUserInfoDto
{
    /// <summary>Gets the authenticated user's database identifier.</summary>
    public long UserId { get; init; }

    /// <summary>Gets the authenticated user's username.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>Gets whether the authenticated user holds superuser privileges.</summary>
    public bool IsSuperuser { get; init; }
}

/// <summary>
/// Physical storage metadata mirrored from server <c>Core.Files.Models.StorageEntryMetadata</c>.
/// </summary>
public sealed record StorageEntryMetadataInfoDto
{
    /// <summary>Gets the storage provider name (e.g., <c>Local</c>).</summary>
    public string StorageProvider { get; init; } = string.Empty;

    /// <summary>Gets the canonical physical path or URI.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>Gets the containing directory or prefix.</summary>
    public string Directory { get; init; } = string.Empty;

    /// <summary>Gets the stored file name, if applicable.</summary>
    public string? FileName { get; init; }

    /// <summary>Gets the file size in bytes.</summary>
    public long SizeBytes { get; init; }

    /// <summary>Gets the creation UTC timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Storage entry wrapper returned by <c>GET /api/v1/samples</c> and <c>GET /api/v1/storage-entry-metadata</c>.
/// </summary>
public sealed record StorageEntryResultDto
{
    /// <summary>Gets the database metadata entity identifier.</summary>
    public long EntityId { get; init; }

    /// <summary>Gets the physical storage metadata.</summary>
    public StorageEntryMetadataInfoDto Metadata { get; init; } = new();
}

/// <summary>
/// Paired knee ligament sample dataset constructed from <c>GET /api/v1/samples</c> storage entries.
/// </summary>
public sealed record SampleDatasetDto
{
    /// <summary>Gets the unique paired sample identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the human-readable dataset name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the resolved storage location of the raw strain CSV file.</summary>
    public string StrainFileLocation { get; init; } = string.Empty;

    /// <summary>Gets the resolved storage location of the raw stress CSV file.</summary>
    public string StressFileLocation { get; init; } = string.Empty;

    /// <summary>Gets the database entity ID of the strain CSV.</summary>
    public long StrainEntityId { get; init; }

    /// <summary>Gets the database entity ID of the stress CSV.</summary>
    public long StressEntityId { get; init; }
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
/// Experimental group payload for single or batch pipeline execution.
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

    /// <summary>Gets the target mechanical constitutive model name.</summary>
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
/// Request payload mirroring <c>ExecuteBatchExperimentalDataPipelineRequest</c>.
/// </summary>
public sealed record ExecuteBatchExperimentalDataRequestDto
{
    /// <summary>Gets the experimental strain/stress groups to process in batch.</summary>
    public required IReadOnlyList<ExperimentalDataGroupDto> Groups { get; init; }

    /// <summary>Gets the target mechanical constitutive model name.</summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>Gets the target segment types to fit.</summary>
    public IReadOnlyList<string> TargetSegments { get; init; } = ["Relaxation"];

    /// <summary>Gets the curve fitter engine name (<c>Alglib</c> or <c>MathNet</c>).</summary>
    public string CurveFitterType { get; init; } = "Alglib";

    /// <summary>Gets the target R-squared determination coefficient threshold.</summary>
    public double TargetRSquared { get; init; } = 0.90;

    /// <summary>Gets the execution mode (<c>Sequential</c> or <c>Parallel</c>).</summary>
    public string ExecutionMode { get; init; } = "Sequential";

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
/// <param name="InitialPoint">Lower bound or initial time point.</param>
/// <param name="FinalPoint">Upper bound or final time point.</param>
/// <param name="Tolerance">Numerical convergence tolerance.</param>
/// <param name="MaxIterations">Maximum solver iterations.</param>
public sealed record RootFindingInputDto(double InitialPoint, double FinalPoint, double Tolerance, int MaxIterations);

/// <summary>
/// Request payload for <c>POST /api/v1/parametric-sweeps/process</c> using explicit baseline parameters.
/// </summary>
public sealed record ExecuteDirectParametricSweepRequestDto
{
    /// <summary>Gets the optional predefined sweep identifier.</summary>
    public string? Identifier { get; init; }

    /// <summary>Gets the target mechanical constitutive model name.</summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>Gets the polymorphic baseline constitutive parameters JSON (must include <c>$type</c>).</summary>
    public required JsonElement Baseline { get; init; }

    /// <summary>Gets the baseline strain value.</summary>
    public double BaseStrain { get; init; } = 0.04;

    /// <summary>Gets the optional strain sweep range.</summary>
    public RangeParametersDto? StrainRange { get; init; }

    /// <summary>Gets optional scalar constitutive variable sweep ranges.</summary>
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
/// Request payload for <c>POST /api/v1/parametric-sweeps/process/experimental-data</c>.
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
/// Registered constitutive model metadata returned by <c>GET /api/v1/mechanics/models</c>.
/// </summary>
public sealed record MechanicalModelMetadataDto
{
    /// <summary>Gets the registered mechanical model name.</summary>
    public string MechanicalModelName { get; init; } = string.Empty;

    /// <summary>Gets the calculator service type name.</summary>
    public string CalculatorTypeName { get; init; } = string.Empty;

    /// <summary>Gets an example JSON payload for the model's constitutive parameters.</summary>
    public JsonElement ConstitutiveParameters { get; init; }

    /// <summary>Gets an example JSON payload for the model's output.</summary>
    public JsonElement Output { get; init; }
}

/// <summary>
/// Mechanical boundary parameter value wrapper for specimen analysis.
/// </summary>
public sealed record MechanicalParameterDto
{
    /// <summary>Gets the scalar value of the independent mechanical parameter.</summary>
    public double Value { get; init; }
}

/// <summary>
/// Control variable and initial dependent value for specimen analysis.
/// </summary>
public sealed record AnalysisParameterDto
{
    /// <summary>Gets the initial value of the dependent variable.</summary>
    public double InitialDependentMechanicalParameterValue { get; init; }

    /// <summary>Gets the independent mechanical parameter.</summary>
    public required MechanicalParameterDto IndependentMechanicalParameter { get; init; }
}

/// <summary>
/// Request payload for <c>POST /api/v1/mechanics/specimen-analysis/execute</c>.
/// </summary>
public sealed record ExecuteSpecimenAnalysisRequestDto
{
    /// <summary>Gets the registered mechanical model name.</summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>Gets the optional correlation identifier.</summary>
    public string? AnalysisIdentifier { get; init; }

    /// <summary>Gets the boundary condition parameters.</summary>
    public required AnalysisParameterDto AnalysisParameter { get; init; }

    /// <summary>Gets the load-response relationship (<c>StressStrain</c> or <c>ForceDisplacement</c>).</summary>
    public string LoadResponseRelationship { get; init; } = "StressStrain";

    /// <summary>Gets the viscoelastic phenomenon (<c>Relaxation</c> or <c>Creep</c>).</summary>
    public string ViscoelasticEffect { get; init; } = "Relaxation";

    /// <summary>Gets the ramp-time consideration (<c>Disregard</c> or <c>Consider</c>).</summary>
    public string RampTimeConsideration { get; init; } = "Disregard";

    /// <summary>Gets the optional ramp duration in seconds.</summary>
    public double? RampTime { get; init; }

    /// <summary>Gets the initial time in seconds.</summary>
    public double? InitialTime { get; init; } = 0.0;

    /// <summary>Gets the discrete time step in seconds.</summary>
    public double TimeStep { get; init; } = 0.5;

    /// <summary>Gets the final time in seconds.</summary>
    public double FinalTime { get; init; } = 10.0;

    /// <summary>Gets the constitutive parameters JSON object for the selected model.</summary>
    public required JsonElement ConstitutiveParameters { get; init; }
}

/// <summary>
/// Result payload returned by <c>POST /api/v1/mechanics/specimen-analysis/execute</c>.
/// </summary>
public sealed record ExecuteSpecimenAnalysisResultDto
{
    /// <summary>Gets the serialized initial, final, absolute delta, and percentage delta simulation outputs.</summary>
    public JsonElement DeltaOutput { get; init; }

    /// <summary>Gets the detected asymptote convergence time in seconds, if reached.</summary>
    public double? AsymptoteTime { get; init; }

    /// <summary>Gets the database metadata entity ID of the persisted output CSV.</summary>
    public long OutputMetadataEntityId { get; init; }
}

/// <summary>
/// Polymorphic cross-section profile payload for fatigue calculations.
/// </summary>
public sealed record FatigueProfileDto
{
    /// <summary>Gets the JSON polymorphic type discriminator (<c>Circular</c> or <c>Rectangular</c>).</summary>
    [JsonPropertyName("$type")]
    public required string Type { get; init; }

    /// <summary>Gets the circular diameter in mm (when <see cref="Type"/> is <c>Circular</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Diameter { get; init; }

    /// <summary>Gets the rectangular width in mm (when <see cref="Type"/> is <c>Rectangular</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Width { get; init; }

    /// <summary>Gets the rectangular height in mm (when <see cref="Type"/> is <c>Rectangular</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Height { get; init; }

    /// <summary>Gets the optional wall thickness in mm for hollow sections.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Thickness { get; init; }
}

/// <summary>
/// Request payload for <c>POST /api/v1/mechanics/fatigue/calculate</c> and <c>/endurance-limit</c>.
/// </summary>
public sealed record FatigueInputDto
{
    /// <summary>Gets the minimum applied stress (MPa).</summary>
    public double MinimumAppliedStress { get; init; }

    /// <summary>Gets the maximum applied stress (MPa).</summary>
    public double MaximumAppliedStress { get; init; }

    /// <summary>Gets the ultimate tensile strength Sut (MPa).</summary>
    public double TensileStress { get; init; }

    /// <summary>Gets the uncorrected rotating-beam endurance limit Se' (MPa).</summary>
    public double FatigueLimit { get; init; }

    /// <summary>Gets the fatigue strength fraction f (dimensionless).</summary>
    public double FatigueLimitFraction { get; init; }

    /// <summary>Gets whether the section rotates under bending.</summary>
    public bool IsRotativeSection { get; init; }

    /// <summary>Gets the target reliability enum name.</summary>
    public string Reliability { get; init; } = "NinetyNine";

    /// <summary>Gets the surface finish enum name.</summary>
    public string SurfaceFinish { get; init; } = "Machined";

    /// <summary>Gets the loading type enum name.</summary>
    public string LoadingType { get; init; } = "Bending";

    /// <summary>Gets the operating temperature in Celsius.</summary>
    public double Temperature { get; init; } = 25.0;

    /// <summary>Gets the cross-section profile definition.</summary>
    public required FatigueProfileDto Profile { get; init; }
}

/// <summary>
/// Output payload returned by <c>POST /api/v1/mechanics/fatigue/calculate</c>.
/// </summary>
public sealed record FatigueOutputDto
{
    /// <summary>Gets the Modified Goodman fatigue safety factor.</summary>
    public double SafetyFactor { get; init; }

    /// <summary>Gets the stress amplitude (MPa).</summary>
    public double StressAmplitude { get; init; }

    /// <summary>Gets the mean stress (MPa).</summary>
    public double MeanStress { get; init; }

    /// <summary>Gets the equivalent completely reversed stress (MPa).</summary>
    public double EquivalentStress { get; init; }

    /// <summary>Gets the estimated number of cycles to failure.</summary>
    public double NumberOfCycles { get; init; }
}

/// <summary>
/// Unified plugin representation mirroring server <c>PluginInfo</c> across catalog discovery (<c>GET /api/v1/plugins</c>),
/// community submission/moderation workflows (<c>/submissions</c>, <c>/upload</c>),
/// and superuser storage inspection (<c>GET /api/v1/plugins/inspect</c>).
/// </summary>
public sealed record PluginInfoDto
{
    /// <summary>Gets the logical plugin name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the semantic version string (e.g., <c>v1.0.0</c>), if valid.</summary>
    public string? Version { get; init; }

    /// <summary>Gets whether every processable type in the plugin has been registered in DI.</summary>
    public bool? IsFullyLoaded { get; init; }

    /// <summary>Gets the UTC timestamp when registration completed, if loaded.</summary>
    public DateTimeOffset? FullyLoadedAt { get; init; }

    /// <summary>Gets the map of discovered processable type names to their DI load status.</summary>
    public Dictionary<string, bool>? TypesLoadedStatus { get; init; }

    /// <summary>Gets the canonical plugin storage partition where the inspected file resides.</summary>
    public string? Partition { get; init; }

    /// <summary>Gets the physical storage metadata (<c>StorageEntryMetadata</c>) of the inspected plugin file.</summary>
    public StorageEntryMetadataInfoDto? File { get; init; }

    /// <summary>Gets whether the inspected file passed naming, PE/CLI metadata, and host architecture validation.</summary>
    public bool? IsValidAssembly { get; init; }

    /// <summary>Gets the validation or load error message when invalid.</summary>
    public string? ValidationError { get; init; }

    /// <summary>Gets the submission database identifier.</summary>
    public long? EntityId { get; init; }

    /// <summary>Gets the author user ID, or <see langword="null"/> for system-owned plugins.</summary>
    public long? UserEntityId { get; init; }

    /// <summary>Gets the associated <c>storage_entry_metadata</c> identifier.</summary>
    public long? StorageEntryMetadataEntityId { get; init; }

    /// <summary>Gets the SHA-256 hex digest of the assembly.</summary>
    public string? Sha256Hash { get; init; }

    /// <summary>Gets the moderation status (<c>Pending</c>, <c>Approved</c>, <c>Rejected</c>).</summary>
    public string? Status { get; init; }

    /// <summary>Gets the creation UTC timestamp.</summary>
    public DateTimeOffset? CreationTimestamp { get; init; }

    /// <summary>Gets the reviewing superuser identifier, if reviewed.</summary>
    public long? ReviewedByUserEntityId { get; init; }

    /// <summary>Gets the review UTC timestamp, if reviewed.</summary>
    public DateTimeOffset? ReviewedAt { get; init; }

    /// <summary>Gets optional reviewer notes.</summary>
    public string? ReviewNotes { get; init; }
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
/// <param name="InitialStress">Initial stress at t0.</param>
/// <param name="FinalStress">Final stress at tf.</param>
/// <param name="PercentageDeltaStress">Relative stress relaxation percentage.</param>
public sealed record SensitivityPoint(double VariableValue, double InitialStress, double FinalStress, double PercentageDeltaStress);
