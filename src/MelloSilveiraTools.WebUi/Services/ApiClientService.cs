using MelloSilveiraTools.WebUi.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.WebUi.Services;

/// <summary>
/// Typed HTTP client encapsulating every <c>MelloSilveiraTools.WebApi.Host</c> surface:
/// JWE authentication, sample datasets, experimental data single/batch pipelines,
/// parametric sensitivity sweeps, NDJSON streaming, CRUD queries, Mechanics of Materials,
/// storage entry metadata, and dynamic plugin management.
/// </summary>
public sealed class ApiClientService(HttpClient httpClient)
{
    private const long MaxUploadSizeBytes = 50L * 1024 * 1024;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private string? _bearerToken;

    /// <summary>Gets the current JWE access token if authenticated.</summary>
    public string? CurrentToken => _bearerToken;

    /// <summary>Gets the expiration timestamp of the current JWE access token.</summary>
    public DateTimeOffset? TokenExpiresOn { get; private set; }

    /// <summary>Gets the authenticated caller's live identity and superuser status.</summary>
    public CurrentUserInfoDto? CurrentUser { get; private set; }

    /// <summary>Gets whether the client currently holds a JWE bearer token.</summary>
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_bearerToken);

    /// <summary>Gets whether the authenticated caller currently holds superuser privileges.</summary>
    public bool IsSuperuser => CurrentUser?.IsSuperuser == true;

    /// <summary>Authenticates an existing user via <c>POST /api/v1/auth/login</c>.</summary>
    public Task<ApiResultDto<AuthTokenDto>> LoginAsync(string username, string password, CancellationToken cancellationToken = default) =>
        AuthenticateAsync("/api/v1/auth/login", username, password, cancellationToken);

    /// <summary>Registers a new user via <c>POST /api/v1/auth/register</c>.</summary>
    public Task<ApiResultDto<AuthTokenDto>> RegisterAsync(string username, string password, CancellationToken cancellationToken = default) =>
        AuthenticateAsync("/api/v1/auth/register", username, password, cancellationToken);

    /// <summary>Refreshes the active JWE token via <c>POST /api/v1/auth/refresh</c>.</summary>
    public async Task<ApiResultDto<AuthTokenDto>> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_bearerToken))
            return new ApiResultDto<AuthTokenDto> { Success = false, Messages = ["No active token to refresh."] };

        using HttpResponseMessage response = await httpClient
            .PostAsJsonAsync("/api/v1/auth/refresh", new { Token = _bearerToken }, cancellationToken)
            .ConfigureAwait(false);

        ApiResultDto<AuthTokenDto> result = await ReadApiResultSafeAsync<AuthTokenDto>(response, cancellationToken).ConfigureAwait(false);
        if (result.Success && result.Data is not null && !string.IsNullOrWhiteSpace(result.Data.Value))
        {
            SetToken(result.Data);
            CurrentUser = await FetchCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>Validates the active JWE token via <c>POST /api/v1/auth/validate</c>.</summary>
    public async Task<ApiResultDto<object>> ValidateTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_bearerToken))
            return new ApiResultDto<object> { Success = false, Messages = ["No active token to validate."] };

        using HttpResponseMessage response = await httpClient
            .PostAsJsonAsync("/api/v1/auth/validate", new { Token = _bearerToken }, cancellationToken)
            .ConfigureAwait(false);

        return await ReadApiResultSafeAsync<object>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Clears the active JWE bearer token and cached user identity.</summary>
    public void Logout()
    {
        _bearerToken = null;
        TokenExpiresOn = null;
        CurrentUser = null;
        httpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Retrieves pre-bundled knee ligament sample files from <c>GET /api/v1/samples</c>
    /// and pairs matching <c>*_strain.csv</c> / <c>*_stress.csv</c> entries.
    /// </summary>
    public async Task<List<SampleDatasetDto>> GetSamplesAsync(CancellationToken cancellationToken = default)
    {
        ApiResultDto<List<StorageEntryResultDto>> result = await GetApiResultAsync<List<StorageEntryResultDto>>("/api/v1/samples", cancellationToken).ConfigureAwait(false);
        if (!result.Success || result.Data is null || result.Data.Count == 0)
            return [];

        Dictionary<string, (StorageEntryResultDto? Strain, StorageEntryResultDto? Stress)> pairs = new(StringComparer.OrdinalIgnoreCase);

        foreach (StorageEntryResultDto entry in result.Data)
        {
            string? fileName = entry.Metadata.FileName;
            if (string.IsNullOrWhiteSpace(fileName))
                continue;

            if (fileName.EndsWith("_strain.csv", StringComparison.OrdinalIgnoreCase))
            {
                string key = fileName[..^"_strain.csv".Length];
                pairs.TryGetValue(key, out (StorageEntryResultDto? Strain, StorageEntryResultDto? Stress) current);
                pairs[key] = (entry, current.Stress);
            }
            else if (fileName.EndsWith("_stress.csv", StringComparison.OrdinalIgnoreCase))
            {
                string key = fileName[..^"_stress.csv".Length];
                pairs.TryGetValue(key, out (StorageEntryResultDto? Strain, StorageEntryResultDto? Stress) current);
                pairs[key] = (current.Strain, entry);
            }
        }

        List<SampleDatasetDto> datasets = [];
        foreach (KeyValuePair<string, (StorageEntryResultDto? Strain, StorageEntryResultDto? Stress)> pair in pairs.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (pair.Value.Strain is null || pair.Value.Stress is null)
                continue;

            datasets.Add(new SampleDatasetDto
            {
                Id = pair.Key,
                Name = pair.Key.Replace('_', ' '),
                StrainFileLocation = pair.Value.Strain.Metadata.Location,
                StressFileLocation = pair.Value.Stress.Metadata.Location,
                StrainEntityId = pair.Value.Strain.EntityId,
                StressEntityId = pair.Value.Stress.EntityId
            });
        }

        return datasets;
    }

    /// <summary>Uploads raw <c>strain.csv</c> and <c>stress.csv</c> files via <c>POST /api/v1/experimental-data/upload</c>.</summary>
    public async Task<ApiResultDto<ExperimentalUploadResponseDto>> UploadExperimentalCsvAsync(
        IBrowserFile strainFile,
        IBrowserFile stressFile,
        CancellationToken cancellationToken = default)
    {
        using MultipartFormDataContent formData = new();
        await using Stream strainStream = strainFile.OpenReadStream(MaxUploadSizeBytes, cancellationToken);
        await using Stream stressStream = stressFile.OpenReadStream(MaxUploadSizeBytes, cancellationToken);

        using StreamContent strainContent = new(strainStream);
        strainContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(strainContent, "strainFile", strainFile.Name);

        using StreamContent stressContent = new(stressStream);
        stressContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(stressContent, "stressFile", stressFile.Name);

        using HttpResponseMessage response = await httpClient.PostAsync("/api/v1/experimental-data/upload", formData, cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<ExperimentalUploadResponseDto>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Starts a single experimental fitting pipeline via <c>POST /api/v1/experimental-data/process</c>.</summary>
    public Task<ApiResultDto<string>> StartExperimentalPipelineAsync(
        ExecuteExperimentalDataRequestDto request,
        CancellationToken cancellationToken = default) =>
        PostJsonAsync<ExecuteExperimentalDataRequestDto, string>("/api/v1/experimental-data/process", request, cancellationToken);

    /// <summary>Starts a batch of experimental fitting pipelines via <c>POST /api/v1/experimental-data/process/batch</c> and collects streamed pipeline IDs.</summary>
    public Task<List<string>> StartExperimentalBatchPipelineAsync(
        ExecuteBatchExperimentalDataRequestDto request,
        CancellationToken cancellationToken = default) =>
        PostNdjsonStringsAsync("/api/v1/experimental-data/process/batch", request, cancellationToken);

    /// <summary>Streams real-time NDJSON progress events from <c>GET /api/v1/pipelines/{pipelineId}/progress</c>.</summary>
    public async IAsyncEnumerable<PipelineProgressEventDto> StreamPipelineProgressAsync(
        string pipelineId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/pipelines/{Uri.EscapeDataString(pipelineId)}/progress");
        using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using StreamReader reader = new(stream, Encoding.UTF8);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                yield break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            PipelineProgressEventDto? progressEvent = JsonSerializer.Deserialize<PipelineProgressEventDto>(line, _jsonOptions);
            if (progressEvent is not null)
                yield return progressEvent;
        }
    }

    /// <summary>Queries calibrated curve fits via <c>GET /api/v1/curve-fit-outputs</c>.</summary>
    public async Task<List<MechanicalModelCurveFitOutputDto>> GetCurveFitsAsync(string? externalIdentifier = null, CancellationToken cancellationToken = default)
    {
        string url = BuildQueryUrl("/api/v1/curve-fit-outputs", [
            ("ExternalIdentifier", externalIdentifier),
            ("PageNumber", "1"),
            ("PageSize", "50")
        ]);
        ApiPagedResultDto<MechanicalModelCurveFitOutputDto>? result = await GetPagedResultSafeAsync<MechanicalModelCurveFitOutputDto>(url, cancellationToken).ConfigureAwait(false);
        return result?.Data ?? [];
    }

    /// <summary>Queries mechanical model simulations via <c>GET /api/v1/mechanical-model-simulations</c>.</summary>
    public async Task<List<MechanicalModelSimulationDto>> GetSimulationsAsync(string? externalIdentifier = null, CancellationToken cancellationToken = default)
    {
        string url = BuildQueryUrl("/api/v1/mechanical-model-simulations", [
            ("ExternalIdentifier", externalIdentifier),
            ("PageNumber", "1"),
            ("PageSize", "50")
        ]);
        ApiPagedResultDto<MechanicalModelSimulationDto>? result = await GetPagedResultSafeAsync<MechanicalModelSimulationDto>(url, cancellationToken).ConfigureAwait(false);
        return result?.Data ?? [];
    }

    /// <summary>Queries parametric sweeps via <c>GET /api/v1/parametric-sweeps</c>.</summary>
    public async Task<List<ParametricSweepDto>> GetParametricSweepsAsync(string? externalIdentifier = null, CancellationToken cancellationToken = default)
    {
        string url = BuildQueryUrl("/api/v1/parametric-sweeps", [
            ("ExternalIdentifier", externalIdentifier),
            ("PageNumber", "1"),
            ("PageSize", "50")
        ]);
        ApiPagedResultDto<ParametricSweepDto>? result = await GetPagedResultSafeAsync<ParametricSweepDto>(url, cancellationToken).ConfigureAwait(false);
        return result?.Data ?? [];
    }

    /// <summary>Deletes a persisted CRUD entity by route prefix and identifier.</summary>
    public async Task<ApiResultDto<object>> DeleteCrudEntityAsync(string resourceRoute, long id, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.DeleteAsync($"{resourceRoute.TrimEnd('/')}/{id}", cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<object>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Starts a direct OAAT parametric sweep via <c>POST /api/v1/parametric-sweeps/process</c>.</summary>
    public Task<ApiResultDto<string>> StartDirectParametricSweepAsync(
        ExecuteDirectParametricSweepRequestDto request,
        CancellationToken cancellationToken = default) =>
        PostJsonAsync<ExecuteDirectParametricSweepRequestDto, string>("/api/v1/parametric-sweeps/process", request, cancellationToken);

    /// <summary>Dispatches OAAT sweeps from experimental curve fits via <c>POST /api/v1/parametric-sweeps/process/experimental-data</c>.</summary>
    public Task<List<string>> StartParametricSweepFromExperimentalDataAsync(
        ExecuteParametricSweepFromExperimentalDataRequestDto request,
        CancellationToken cancellationToken = default) =>
        PostNdjsonStringsAsync("/api/v1/parametric-sweeps/process/experimental-data", request, cancellationToken);

    /// <summary>Lists all registered mechanical constitutive models and their JSON Schemas via <c>GET /api/v1/mechanics/models</c>.</summary>
    public async Task<List<MechanicalModelMetadataDto>> GetMechanicalModelsAsync(CancellationToken cancellationToken = default)
    {
        ApiResultDto<List<MechanicalModelMetadataDto>> result = await GetApiResultAsync<List<MechanicalModelMetadataDto>>("/api/v1/mechanics/models", cancellationToken).ConfigureAwait(false);
        return result.Success && result.Data is not null ? result.Data : [];
    }

    /// <summary>Executes a deterministic specimen analysis via <c>POST /api/v1/mechanics/specimen-analysis/execute</c>.</summary>
    public Task<ApiResultDto<ExecuteSpecimenAnalysisResultDto>> ExecuteSpecimenAnalysisAsync(
        ExecuteSpecimenAnalysisRequestDto request,
        CancellationToken cancellationToken = default) =>
        PostJsonAsync<ExecuteSpecimenAnalysisRequestDto, ExecuteSpecimenAnalysisResultDto>("/api/v1/mechanics/specimen-analysis/execute", request, cancellationToken);

    /// <summary>Executes a full Goodman/Marin fatigue analysis via <c>POST /api/v1/mechanics/fatigue/calculate</c>.</summary>
    public Task<ApiResultDto<FatigueOutputDto>> CalculateFatigueAsync(
        FatigueInputDto request,
        CancellationToken cancellationToken = default) =>
        PostJsonAsync<FatigueInputDto, FatigueOutputDto>("/api/v1/mechanics/fatigue/calculate", request, cancellationToken);

    /// <summary>Calculates the Marin-corrected endurance limit Se (MPa) via <c>POST /api/v1/mechanics/fatigue/calculate/endurance-limit</c>.</summary>
    public Task<ApiResultDto<double>> CalculateEnduranceLimitAsync(
        FatigueInputDto request,
        CancellationToken cancellationToken = default) =>
        PostJsonAsync<FatigueInputDto, double>("/api/v1/mechanics/fatigue/calculate/endurance-limit", request, cancellationToken);

    /// <summary>Queries stored file metadata entries via <c>GET /api/v1/storage-entry-metadata</c>.</summary>
    public async Task<List<StorageEntryResultDto>> GetStorageEntriesAsync(
        string? externalIdentifier = null,
        string? identifier = null,
        string? context = null,
        string? storageProvider = null,
        CancellationToken cancellationToken = default)
    {
        string url = BuildQueryUrl("/api/v1/storage-entry-metadata", [
            ("ExternalIdentifier", externalIdentifier),
            ("Identifier", identifier),
            ("Context", context),
            ("StorageProvider", storageProvider)
        ]);
        ApiResultDto<List<StorageEntryResultDto>> result = await GetApiResultAsync<List<StorageEntryResultDto>>(url, cancellationToken).ConfigureAwait(false);
        return result.Success && result.Data is not null ? result.Data : [];
    }

    /// <summary>Uploads a general file into storage via <c>POST /api/v1/storage-entry-metadata</c>.</summary>
    public async Task<ApiResultDto<StorageEntryResultDto>> UploadStorageEntryAsync(
        IBrowserFile file,
        string identifier,
        string context,
        string? externalIdentifier = null,
        string? storageProvider = null,
        CancellationToken cancellationToken = default)
    {
        using MultipartFormDataContent formData = new();
        await using Stream stream = file.OpenReadStream(MaxUploadSizeBytes, cancellationToken);
        using StreamContent fileContent = new(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        formData.Add(fileContent, "file", file.Name);
        formData.Add(new StringContent(identifier), "identifier");
        formData.Add(new StringContent(context), "context");
        if (!string.IsNullOrWhiteSpace(externalIdentifier))
            formData.Add(new StringContent(externalIdentifier), "externalIdentifier");
        if (!string.IsNullOrWhiteSpace(storageProvider))
            formData.Add(new StringContent(storageProvider), "storageProvider");

        using HttpResponseMessage response = await httpClient.PostAsync("/api/v1/storage-entry-metadata", formData, cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<StorageEntryResultDto>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Overwrites an existing stored file via <c>PUT /api/v1/storage-entry-metadata/{fileId}</c>.</summary>
    public async Task<ApiResultDto<StorageEntryResultDto>> UpdateStorageEntryFileAsync(
        long fileId,
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        using MultipartFormDataContent formData = new();
        await using Stream stream = file.OpenReadStream(MaxUploadSizeBytes, cancellationToken);
        using StreamContent fileContent = new(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        formData.Add(fileContent, "file", file.Name);

        using HttpResponseMessage response = await httpClient.PutAsync($"/api/v1/storage-entry-metadata/{fileId}", formData, cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<StorageEntryResultDto>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a stored file and its metadata via <c>DELETE /api/v1/storage-entry-metadata/{fileId}</c>.</summary>
    public async Task<ApiResultDto<object>> DeleteStorageEntryAsync(long fileId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.DeleteAsync($"/api/v1/storage-entry-metadata/{fileId}", cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<object>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Downloads a text/CSV artifact via <c>GET /api/v1/storage-entry-metadata/{fileId}/download</c>.</summary>
    public Task<string> DownloadStoredFileCsvAsync(long fileId, CancellationToken cancellationToken = default) =>
        httpClient.GetStringAsync($"/api/v1/storage-entry-metadata/{fileId}/download", cancellationToken);

    /// <summary>Lists sanitized loaded runtime plugins via <c>GET /api/v1/plugins</c>.</summary>
    public async Task<List<PluginInfoDto>> GetPluginsAsync(
        string? name = null,
        string? version = null,
        bool? fullyLoaded = null,
        CancellationToken cancellationToken = default)
    {
        string url = BuildQueryUrl("/api/v1/plugins", [
            ("Name", name),
            ("Version", version),
            ("FullyLoaded", fullyLoaded?.ToString())
        ]);
        ApiResultDto<List<PluginInfoDto>> result = await GetApiResultAsync<List<PluginInfoDto>>(url, cancellationToken).ConfigureAwait(false);
        return result.Success && result.Data is not null ? result.Data : [];
    }

    /// <summary>Inspects plugin assemblies across main storage partitions via <c>GET /api/v1/plugins/inspect</c> (superuser only).</summary>
    public Task<ApiResultDto<List<PluginInfoDto>>> InspectPluginsAsync(
        string? name = null,
        string? version = null,
        string? partition = null,
        CancellationToken cancellationToken = default)
    {
        string url = BuildQueryUrl("/api/v1/plugins/inspect", [
            ("Name", name),
            ("Version", version),
            ("Partition", partition)
        ]);
        return GetApiResultAsync<List<PluginInfoDto>>(url, cancellationToken);
    }

    /// <summary>Submits a community plugin DLL into <c>pending/</c> via <c>POST /api/v1/plugins/submissions</c>.</summary>
    public Task<ApiResultDto<PluginInfoDto>> SubmitPluginAsync(IBrowserFile file, CancellationToken cancellationToken = default) =>
        UploadPluginFileAsync("/api/v1/plugins/submissions", file, cancellationToken);

    /// <summary>Publishes a plugin DLL directly into <c>incoming/</c> (superuser only) via <c>POST /api/v1/plugins/upload</c>.</summary>
    public Task<ApiResultDto<PluginInfoDto>> UploadPluginAsSuperuserAsync(IBrowserFile file, CancellationToken cancellationToken = default) =>
        UploadPluginFileAsync("/api/v1/plugins/upload", file, cancellationToken);

    /// <summary>Lists visible plugin submissions via <c>GET /api/v1/plugins/submissions</c>.</summary>
    public async Task<ApiResultDto<List<PluginInfoDto>>> GetPluginSubmissionsAsync(
        string? pluginName = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        string url = BuildQueryUrl("/api/v1/plugins/submissions", [
            ("PluginName", pluginName),
            ("Status", status)
        ]);
        return await GetApiResultAsync<List<PluginInfoDto>>(url, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cancels a pending plugin submission via <c>DELETE /api/v1/plugins/submissions/{id}</c>.</summary>
    public async Task<ApiResultDto<object>> CancelPluginSubmissionAsync(long id, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.DeleteAsync($"/api/v1/plugins/submissions/{id}", cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<object>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Approves a pending plugin submission (superuser only) via <c>POST /api/v1/plugins/submissions/{id}/approve</c>.</summary>
    public Task<ApiResultDto<PluginInfoDto>> ApprovePluginSubmissionAsync(long id, string? reviewNotes, CancellationToken cancellationToken = default) =>
        PostJsonAsync<object, PluginInfoDto>($"/api/v1/plugins/submissions/{id}/approve", new { ReviewNotes = reviewNotes }, cancellationToken);

    /// <summary>Rejects a pending plugin submission (superuser only) via <c>POST /api/v1/plugins/submissions/{id}/reject</c>.</summary>
    public Task<ApiResultDto<PluginInfoDto>> RejectPluginSubmissionAsync(long id, string? reviewNotes, CancellationToken cancellationToken = default) =>
        PostJsonAsync<object, PluginInfoDto>($"/api/v1/plugins/submissions/{id}/reject", new { ReviewNotes = reviewNotes }, cancellationToken);

    /// <summary>Triggers plugin loading via <c>POST /api/v1/plugins/load</c> or <c>/load/all</c> (superuser only).</summary>
    public Task<ApiResultDto<object>> LoadPluginsAsync(string? name = null, string? version = null, CancellationToken cancellationToken = default)
    {
        string url = string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(version)
            ? "/api/v1/plugins/load/all"
            : BuildQueryUrl("/api/v1/plugins/load", [("Name", name), ("Version", version)]);
        return SendEmptyAsync(HttpMethod.Post, url, cancellationToken);
    }

    /// <summary>Triggers plugin reloading via <c>POST /api/v1/plugins/reload</c> or <c>/reload/all</c> (superuser only).</summary>
    public Task<ApiResultDto<object>> ReloadPluginsAsync(string? name = null, string? version = null, bool force = false, CancellationToken cancellationToken = default)
    {
        string url = string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(version) && !force
            ? "/api/v1/plugins/reload/all"
            : BuildQueryUrl("/api/v1/plugins/reload", [("Name", name), ("Version", version), ("Force", force ? "true" : null)]);
        return SendEmptyAsync(HttpMethod.Post, url, cancellationToken);
    }

    /// <summary>Clears the in-memory plugin cache via <c>DELETE /api/v1/plugins/cache</c> (superuser only).</summary>
    public Task<ApiResultDto<object>> ClearPluginCacheAsync(CancellationToken cancellationToken = default) =>
        SendEmptyAsync(HttpMethod.Delete, "/api/v1/plugins/cache", cancellationToken);

    /// <summary>Persists the plugin cache via <c>POST /api/v1/plugins/cache/{target}/persist</c> (superuser only).</summary>
    public Task<ApiResultDto<object>> PersistPluginCacheAsync(string target, string? name = null, string? version = null, CancellationToken cancellationToken = default)
    {
        string url = BuildQueryUrl($"/api/v1/plugins/cache/{Uri.EscapeDataString(target)}/persist", [("Name", name), ("Version", version)]);
        return SendEmptyAsync(HttpMethod.Post, url, cancellationToken);
    }

    /// <summary>Restores the plugin cache via <c>POST /api/v1/plugins/cache/{target}/restore</c> (superuser only).</summary>
    public Task<ApiResultDto<object>> RestorePluginCacheAsync(string target, string? name = null, string? version = null, CancellationToken cancellationToken = default)
    {
        string url = BuildQueryUrl($"/api/v1/plugins/cache/{Uri.EscapeDataString(target)}/restore", [("Name", name), ("Version", version)]);
        return SendEmptyAsync(HttpMethod.Post, url, cancellationToken);
    }

    /// <summary>Parses CSV rows from experimental or simulated output files purely for UI chart rendering.</summary>
    public static List<ChartDataPoint> ParseStressStrainCsv(string csvContent)
    {
        List<ChartDataPoint> points = [];
        using StringReader reader = new(csvContent);
        string? header = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(header))
            return points;

        char delimiter = header.Contains(';') ? ';' : ',';
        string[] columns = header.Split(delimiter);
        int timeIdx = Array.FindIndex(columns, c => c.Trim().Equals("Time", StringComparison.OrdinalIgnoreCase));
        int strainIdx = Array.FindIndex(columns, c => c.Trim().Contains("Strain", StringComparison.OrdinalIgnoreCase) && !c.Trim().Contains("Rate", StringComparison.OrdinalIgnoreCase) && !c.Trim().Contains("Acceleration", StringComparison.OrdinalIgnoreCase));
        int stressIdx = Array.FindIndex(columns, c => c.Trim().Contains("Stress", StringComparison.OrdinalIgnoreCase) && !c.Trim().Contains("Rate", StringComparison.OrdinalIgnoreCase) && !c.Trim().Contains("Acceleration", StringComparison.OrdinalIgnoreCase));
        int segmentIdx = Array.FindIndex(columns, c => c.Trim().Equals("SegmentType", StringComparison.OrdinalIgnoreCase));

        if (timeIdx < 0 || stressIdx < 0)
            return points;

        while (reader.ReadLine() is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(delimiter);
            if (parts.Length <= Math.Max(timeIdx, stressIdx))
                continue;

            if (!double.TryParse(parts[timeIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out double time))
                continue;

            if (!double.TryParse(parts[stressIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out double stress))
                continue;

            double strain = 0.0;
            if (strainIdx >= 0 && strainIdx < parts.Length)
                double.TryParse(parts[strainIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out strain);

            string segmentType = segmentIdx >= 0 && segmentIdx < parts.Length ? parts[segmentIdx].Trim() : "Simulation";
            points.Add(new ChartDataPoint(time, strain, stress, segmentType));
        }

        return points;
    }

    /// <summary>Parses OAAT sensitivity CSV rows purely for UI chart rendering.</summary>
    public static List<SensitivityPoint> ParseSensitivityCsv(string csvContent)
    {
        List<SensitivityPoint> points = [];
        using StringReader reader = new(csvContent);
        string? header = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(header))
            return points;

        char delimiter = header.Contains(';') ? ';' : ',';
        while (reader.ReadLine() is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(delimiter);
            if (parts.Length < 5)
                continue;

            if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double variableValue)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double initialStress)
                && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double finalStress)
                && double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double percentageDelta))
            {
                points.Add(new SensitivityPoint(variableValue, initialStress, finalStress, percentageDelta));
            }
        }

        return points;
    }

    private async Task<ApiResultDto<AuthTokenDto>> AuthenticateAsync(string endpoint, string username, string password, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient
            .PostAsJsonAsync(endpoint, new { Username = username, Password = password }, cancellationToken)
            .ConfigureAwait(false);

        ApiResultDto<AuthTokenDto> result = await ReadApiResultSafeAsync<AuthTokenDto>(response, cancellationToken).ConfigureAwait(false);
        if (result.Success && result.Data is not null && !string.IsNullOrWhiteSpace(result.Data.Value))
        {
            SetToken(result.Data);
            CurrentUser = await FetchCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private async Task<CurrentUserInfoDto?> FetchCurrentUserAsync(CancellationToken cancellationToken)
    {
        ApiResultDto<CurrentUserInfoDto> me = await GetApiResultAsync<CurrentUserInfoDto>("/api/v1/auth/me", cancellationToken).ConfigureAwait(false);
        return me.Success ? me.Data : null;
    }

    private async Task<ApiResultDto<PluginInfoDto>> UploadPluginFileAsync(string endpoint, IBrowserFile file, CancellationToken cancellationToken)
    {
        using MultipartFormDataContent formData = new();
        await using Stream stream = file.OpenReadStream(MaxUploadSizeBytes, cancellationToken);
        using StreamContent content = new(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formData.Add(content, "file", file.Name);

        using HttpResponseMessage response = await httpClient.PostAsync(endpoint, formData, cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<PluginInfoDto>(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApiResultDto<T>> GetApiResultAsync<T>(string url, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApiPagedResultDto<T>?> GetPagedResultSafeAsync<T>(string url, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ApiPagedResultDto<T>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApiResultDto<TResponse>> PostJsonAsync<TRequest, TResponse>(string url, TRequest payload, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, payload, _jsonOptions, cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApiResultDto<object>> SendEmptyAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, url);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<object>(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<string>> PostNdjsonStringsAsync<TRequest>(string url, TRequest payload, CancellationToken cancellationToken)
    {
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload, options: _jsonOptions)
        };

        using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ApiResultDto<object> errorResult = await ReadApiResultSafeAsync<object>(response, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorResult.JoinedMessages)
                ? $"Request failed with HTTP {(int)response.StatusCode}."
                : errorResult.JoinedMessages);
        }

        List<string> identifiers = [];
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using StreamReader reader = new(stream, Encoding.UTF8);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string? identifier = JsonSerializer.Deserialize<string>(line, _jsonOptions);
            if (!string.IsNullOrWhiteSpace(identifier))
                identifiers.Add(identifier);
        }

        return identifiers;
    }

    private static string BuildQueryUrl(string baseUrl, IReadOnlyList<(string Key, string? Value)> parameters)
    {
        List<string> queryParts = [];
        foreach ((string key, string? value) in parameters)
        {
            if (!string.IsNullOrWhiteSpace(value))
                queryParts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value.Trim())}");
        }

        return queryParts.Count == 0 ? baseUrl : $"{baseUrl}?{string.Join("&", queryParts)}";
    }

    private static async Task<ApiResultDto<T>> ReadApiResultSafeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string rawBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(rawBody))
        {
            try
            {
                ApiResultDto<T>? parsed = JsonSerializer.Deserialize<ApiResultDto<T>>(rawBody, _jsonOptions);
                if (parsed is not null)
                    return parsed;
            }
            catch (JsonException)
            {
            }
        }

        return new ApiResultDto<T>
        {
            Success = response.IsSuccessStatusCode,
            Messages = [$"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})"]
        };
    }

    private void SetToken(AuthTokenDto authToken)
    {
        _bearerToken = authToken.Value;
        TokenExpiresOn = authToken.ExpiresOn;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Value);
    }
}
