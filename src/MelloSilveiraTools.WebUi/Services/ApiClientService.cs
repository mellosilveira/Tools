using MelloSilveiraTools.WebUi.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MelloSilveiraTools.WebUi.Services;

/// <summary>
/// Typed HTTP client encapsulating JWE authentication, experimental uploads,
/// NDJSON progress streaming, and result queries against <c>MelloSilveiraTools.WebApi.Host</c>.
/// </summary>
public sealed class ApiClientService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private string? _bearerToken;

    /// <summary>
    /// Gets the current JWE access token if authenticated.
    /// </summary>
    public string? CurrentToken => _bearerToken;

    /// <summary>
    /// Gets the expiration timestamp of the current JWE access token.
    /// </summary>
    public DateTimeOffset? TokenExpiresOn { get; private set; }

    /// <summary>
    /// Gets whether the client currently holds a JWE bearer token.
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_bearerToken);

    /// <summary>
    /// Requests a demonstration JWE token from <c>POST /api/v1/auth/demo-token</c> and attaches it as Bearer auth.
    /// </summary>
    public async Task<AuthTokenDto?> AuthenticateDemoAsync(string username = "quickstart-researcher", CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/api/v1/auth/demo-token",
            new { Username = username },
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return null;

        ApiResultDto<AuthTokenDto>? result = await response.Content.ReadFromJsonAsync<ApiResultDto<AuthTokenDto>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result?.Data is null)
            return null;

        SetToken(result.Data);
        return result.Data;
    }

    /// <summary>
    /// Refreshes the active JWE token via <c>POST /api/v1/auth/refresh</c>.
    /// </summary>
    public async Task<AuthTokenDto?> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_bearerToken))
            return null;

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { Token = _bearerToken },
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return null;

        ApiResultDto<AuthTokenDto>? result = await response.Content.ReadFromJsonAsync<ApiResultDto<AuthTokenDto>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        if (result?.Data is null)
            return null;

        SetToken(result.Data);
        return result.Data;
    }

    /// <summary>
    /// Retrieves pre-bundled knee ligament sample datasets from <c>GET /api/v1/samples</c>.
    /// </summary>
    public async Task<List<SampleDatasetDto>> GetSamplesAsync(CancellationToken cancellationToken = default)
    {
        ApiResultDto<List<SampleDatasetDto>>? result = await httpClient.GetFromJsonAsync<ApiResultDto<List<SampleDatasetDto>>>(
            "/api/v1/samples",
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);

        return result?.Data ?? [];
    }

    /// <summary>
    /// Uploads raw <c>strain.csv</c> and <c>stress.csv</c> files via <c>multipart/form-data</c>.
    /// </summary>
    public async Task<ApiResultDto<ExperimentalUploadResponseDto>> UploadExperimentalCsvAsync(
        IBrowserFile strainFile,
        IBrowserFile stressFile,
        CancellationToken cancellationToken = default)
    {
        using MultipartFormDataContent formData = new();

        await using Stream strainStream = strainFile.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024, cancellationToken);
        await using Stream stressStream = stressFile.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024, cancellationToken);

        using StreamContent strainContent = new(strainStream);
        strainContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(strainContent, "strainFile", strainFile.Name);

        using StreamContent stressContent = new(stressStream);
        stressContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        formData.Add(stressContent, "stressFile", stressFile.Name);

        using HttpResponseMessage response = await httpClient.PostAsync("/api/v1/experimental-data/upload", formData, cancellationToken).ConfigureAwait(false);
        return await ReadApiResultSafeAsync<ExperimentalUploadResponseDto>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Triggers the experimental data processing pipeline (<c>POST /api/v1/experimental-data/process</c>) and returns the pipeline identifier.
    /// </summary>
    public async Task<ApiResultDto<string>> StartExperimentalPipelineAsync(
        ExecuteExperimentalDataRequestDto request,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/api/v1/experimental-data/process",
            request,
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);

        return await ReadApiResultSafeAsync<string>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Streams real-time NDJSON progress events from <c>GET /api/v1/pipelines/{pipelineId}/progress</c>.
    /// </summary>
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

    /// <summary>
    /// Queries calibrated constitutive model curve fits by external identifier.
    /// </summary>
    public async Task<List<MechanicalModelCurveFitOutputDto>> GetCurveFitsAsync(string externalIdentifier, CancellationToken cancellationToken = default)
    {
        string url = $"/api/v1/curve-fit-outputs?ExtenalIdentifier={Uri.EscapeDataString(externalIdentifier)}&PageNumber=1&PageSize=50";
        ApiPagedResultDto<MechanicalModelCurveFitOutputDto>? result = await httpClient.GetFromJsonAsync<ApiPagedResultDto<MechanicalModelCurveFitOutputDto>>(
            url,
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);

        return result?.Data ?? [];
    }

    /// <summary>
    /// Queries mechanical model forward simulations by external identifier.
    /// </summary>
    public async Task<List<MechanicalModelSimulationDto>> GetSimulationsAsync(string externalIdentifier, CancellationToken cancellationToken = default)
    {
        string url = $"/api/v1/mechanical-model-simulations?ExternalIdentifier={Uri.EscapeDataString(externalIdentifier)}&PageNumber=1&PageSize=50";
        ApiPagedResultDto<MechanicalModelSimulationDto>? result = await httpClient.GetFromJsonAsync<ApiPagedResultDto<MechanicalModelSimulationDto>>(
            url,
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);

        return result?.Data ?? [];
    }

    /// <summary>
    /// Queries stored file metadata entries by external identifier.
    /// </summary>
    public async Task<List<StorageEntryMetadataDto>> GetStoredFilesAsync(string externalIdentifier, CancellationToken cancellationToken = default)
    {
        string url = $"/api/v1/storage-entry-metadata?ExternalIdentifier={Uri.EscapeDataString(externalIdentifier)}&PageNumber=1&PageSize=100";
        ApiPagedResultDto<StorageEntryMetadataDto>? result = await httpClient.GetFromJsonAsync<ApiPagedResultDto<StorageEntryMetadataDto>>(
            url,
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);

        return result?.Data ?? [];
    }

    /// <summary>
    /// Downloads a CSV artifact by storage entry ID.
    /// </summary>
    public async Task<string> DownloadStoredFileCsvAsync(long fileId, CancellationToken cancellationToken = default)
        => await httpClient.GetStringAsync($"/api/v1/storage-entry-metadata/{fileId}/download", cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Dispatches OAAT parametric sweeps from a completed experimental curve fit run and collects emitted sweep identifiers.
    /// </summary>
    public async Task<List<string>> StartParametricSweepFromExperimentalDataAsync(
        ExecuteParametricSweepFromExperimentalDataRequestDto request,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, "/api/v1/parametric-sweeps/process-from-experimental-data")
        {
            Content = JsonContent.Create(request, options: _jsonOptions)
        };

        using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ApiResultDto<object> errorResult = await ReadApiResultSafeAsync<object>(response, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorResult.JoinedMessages)
                ? $"Parametric sweep failed with HTTP {(int)response.StatusCode}."
                : errorResult.JoinedMessages);
        }

        List<string> sweepIdentifiers = [];
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using StreamReader reader = new(stream, Encoding.UTF8);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string? identifier = JsonSerializer.Deserialize<string>(line, _jsonOptions);
            if (!string.IsNullOrWhiteSpace(identifier))
                sweepIdentifiers.Add(identifier);
        }

        return sweepIdentifiers;
    }

    /// <summary>
    /// Parses CSV rows from experimental or simulated output files purely for UI chart rendering.
    /// </summary>
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

    /// <summary>
    /// Parses OAAT sensitivity CSV rows purely for UI chart rendering.
    /// </summary>
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
        _bearerToken = authToken.Token;
        TokenExpiresOn = authToken.ExpiresOn;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Token);
    }
}
