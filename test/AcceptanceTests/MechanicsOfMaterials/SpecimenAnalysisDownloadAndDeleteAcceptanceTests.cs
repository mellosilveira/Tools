using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcceptanceTests.MechanicsOfMaterials;

/// <summary>
/// End-to-end Kestrel + physical disk acceptance tests verifying that <c>POST /api/v1/mechanics/specimen-analysis/execute</c>
/// streams the generated CSV file to the HTTP client and guarantees physical deletion afterward,
/// including automatic recovery from transient OS/file-lock errors.
/// </summary>
public sealed class SpecimenAnalysisDownloadAndDeleteAcceptanceTests
{
    [Fact]
    public async Task ExecuteSpecimenAnalysis_OverHttpDownload_StreamsRealCsvAndDeletesPhysicalFileFromDisk()
    {
        string tempStorageDir = Path.Combine(Path.GetTempPath(), $"mst-specimen-accept-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempStorageDir);

        try
        {
            await using WebApplication app = await StartHostAsync(tempStorageDir);
            string baseAddress = GetServerBaseAddress(app);

            using HttpClient client = new() { BaseAddress = new Uri(baseAddress) };
            using JsonDocument constitutiveJson = JsonDocument.Parse("""{"youngModulus": 120.0}""");
            ExecuteSpecimenAnalysisRequest request = CreateElasticRequest("accept-download-1", constitutiveJson.RootElement.Clone());

            using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/mechanics/specimen-analysis/execute", request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
            Assert.EndsWith(".csv", response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"'), StringComparison.OrdinalIgnoreCase);

            string csvContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Time", csvContent);
            Assert.True(csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length >= 4);

            // Allow Kestrel response-completion disposal to finish asynchronously after body receipt.
            string[] remainingFiles = await WaitForNoCsvFilesAsync(tempStorageDir, TimeSpan.FromSeconds(5));
            Assert.Empty(remainingFiles);
        }
        finally
        {
            if (Directory.Exists(tempStorageDir))
                Directory.Delete(tempStorageDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteSpecimenAnalysis_WhenInitialDeleteFailsWithTransientLock_RetriesAndDeletesPhysicalFile()
    {
        string tempStorageDir = Path.Combine(Path.GetTempPath(), $"mst-specimen-retry-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempStorageDir);

        try
        {
            TransientLockFileStorageDecorator? storageDecorator = null;
            await using WebApplication app = await StartHostAsync(
                tempStorageDir,
                inner => storageDecorator = new TransientLockFileStorageDecorator(inner, failuresBeforeSuccess: 2));

            string baseAddress = GetServerBaseAddress(app);

            using HttpClient client = new() { BaseAddress = new Uri(baseAddress) };
            using JsonDocument constitutiveJson = JsonDocument.Parse("""{"youngModulus": 120.0}""");
            ExecuteSpecimenAnalysisRequest request = CreateElasticRequest("accept-retry-1", constitutiveJson.RootElement.Clone());

            using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/mechanics/specimen-analysis/execute", request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string csvContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Time", csvContent);

            string[] remainingFiles = await WaitForNoCsvFilesAsync(tempStorageDir, TimeSpan.FromSeconds(5));
            Assert.Empty(remainingFiles);
            Assert.NotNull(storageDecorator);
            Assert.Equal(3, storageDecorator.DeleteAttempts);
        }
        finally
        {
            if (Directory.Exists(tempStorageDir))
                Directory.Delete(tempStorageDir, recursive: true);
        }
    }

    private static ExecuteSpecimenAnalysisRequest CreateElasticRequest(string identifier, JsonElement constitutiveParameters) => new()
    {
        MechanicalModelName = nameof(MechanicalModel.Elastic),
        AnalysisIdentifier = identifier,
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
        ConstitutiveParameters = constitutiveParameters
    };

    private static async Task<WebApplication> StartHostAsync(
        string baseDirectory,
        Func<IFileStorageService, IFileStorageService>? wrapStorage = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        FileStorageSettings storageSettings = new()
        {
            DefaultStorageProvider = StorageProvider.Local,
            Local = new LocalFileStorageSettings { BaseDirectory = baseDirectory }
        };

        ResiliencePipelineSettings resilienceSettings = new()
        {
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Constant,
            DelayInMilliseconds = 50,
            UseJitter = false
        };

        builder.Services.AddCoreServices(
            defaultResiliencePipelineSettings: resilienceSettings,
            fileStorageSettings: storageSettings);
        builder.Services.AddMathematicsServices();
        builder.Services.AddMechanicsOfMaterialsServices(addMechanicalModels: true);

        builder.Services.AddSingleton(provider =>
        {
            IFileStorageService inner = provider.GetRequiredKeyedService<IFileStorageService>(StorageProvider.Local);
            return wrapStorage is null ? inner : wrapStorage(inner);
        });

        WebApplication app = builder.Build();
        app.MapMechanicsOfMaterialsEndpoints();
        await app.StartAsync();
        return app;
    }

    private static string GetServerBaseAddress(WebApplication app)
    {
        IServer server = app.Services.GetRequiredService<IServer>();
        IServerAddressesFeature addresses = server.Features.Get<IServerAddressesFeature>()!;
        return addresses.Addresses.First();
    }

    private static async Task<string[]> WaitForNoCsvFilesAsync(string directory, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        string[] files = Directory.GetFiles(directory, "*.csv", SearchOption.AllDirectories);
        while (files.Length > 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
            files = Directory.GetFiles(directory, "*.csv", SearchOption.AllDirectories);
        }

        return files;
    }

    private sealed class TransientLockFileStorageDecorator(
        IFileStorageService inner,
        int failuresBeforeSuccess) : IFileStorageService
    {
        private int _deleteAttempts;

        public int DeleteAttempts => Volatile.Read(ref _deleteAttempts);

        public StorageProvider Provider => inner.Provider;

        public string ResolveLocation(string identifier, string context, string fileName) => inner.ResolveLocation(identifier, context, fileName);

        public (string Identifier, string Context, string FileName) ParseLocation(string location) => inner.ParseLocation(location);

        public string ExtractDirectory(string location, string? fallbackDirectory = null) => inner.ExtractDirectory(location, fallbackDirectory);

        public ValueTask<Stream> OpenWriteAsync(string location, CancellationToken cancellationToken = default) => inner.OpenWriteAsync(location, cancellationToken);

        public ValueTask<(Stream Stream, string Location)> CreateWriteAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
            => inner.CreateWriteAsync(identifier, context, fileName, cancellationToken);

        public ValueTask<(Stream Stream, string Location)> CreateWriteTimebasedAsync(string identifier, string context, string filePrefix, string fileExtension, CancellationToken cancellationToken = default)
            => inner.CreateWriteTimebasedAsync(identifier, context, filePrefix, fileExtension, cancellationToken);

        public ValueTask<(Stream Stream, string Location)> CreateWriteTimebasedAsync(string identifier, string context, string fileExtension, CancellationToken cancellationToken = default)
            => inner.CreateWriteTimebasedAsync(identifier, context, fileExtension, cancellationToken);

        public ValueTask<Stream> OpenReadAsync(string location, CancellationToken cancellationToken = default) => inner.OpenReadAsync(location, cancellationToken);

        public ValueTask<Stream> OpenReadAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
            => inner.OpenReadAsync(identifier, context, fileName, cancellationToken);

        public ValueTask<bool> ExistsAsync(string location, CancellationToken cancellationToken = default) => inner.ExistsAsync(location, cancellationToken);

        public ValueTask<bool> ExistsAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
            => inner.ExistsAsync(identifier, context, fileName, cancellationToken);

        public ValueTask<StorageEntryMetadata> GetMetadataAsync(string location, CancellationToken cancellationToken = default)
            => inner.GetMetadataAsync(location, cancellationToken);

        public ValueTask<StorageEntryMetadata> GetMetadataAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
            => inner.GetMetadataAsync(identifier, context, fileName, cancellationToken);

        public ValueTask<bool> DeleteAsync(string location, CancellationToken cancellationToken = default)
        {
            int attempt = Interlocked.Increment(ref _deleteAttempts);
            if (attempt <= failuresBeforeSuccess)
                throw new IOException($"Simulated transient OS file lock on attempt {attempt}.");

            return inner.DeleteAsync(location, cancellationToken);
        }

        public ValueTask<bool> DeleteAsync(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
            => inner.DeleteAsync(identifier, context, fileName, cancellationToken);

        public IAsyncEnumerable<StorageEntryMetadata> ListFilesAsync(string identifier, string context, CancellationToken cancellationToken = default)
            => inner.ListFilesAsync(identifier, context, cancellationToken);

        public IAsyncEnumerable<StorageEntryMetadata> ListEntriesAsync(string identifier, string context, CancellationToken cancellationToken = default)
            => inner.ListEntriesAsync(identifier, context, cancellationToken);

        public ValueTask MoveAsync(string sourceLocation, string destinationLocation, bool overwrite = true, CancellationToken cancellationToken = default)
            => inner.MoveAsync(sourceLocation, destinationLocation, overwrite, cancellationToken);

        public ValueTask<string> MoveAsync(string sourceLocation, string destinationIdentifier, string destinationContext, string destinationFileName, bool overwrite = true, CancellationToken cancellationToken = default)
            => inner.MoveAsync(sourceLocation, destinationIdentifier, destinationContext, destinationFileName, overwrite, cancellationToken);

        public ValueTask<string> ComputeSha256Async(string location, CancellationToken cancellationToken = default)
            => inner.ComputeSha256Async(location, cancellationToken);

        public ValueTask<string> ComputeSha256Async(string identifier, string context, string fileName, CancellationToken cancellationToken = default)
            => inner.ComputeSha256Async(identifier, context, fileName, cancellationToken);

        public ValueTask<(string Location, string Directory, string Sha256Hash, long SizeBytes)> WriteAndHashAsync(string identifier, string context, string fileName, Stream sourceStream, CancellationToken cancellationToken = default)
            => inner.WriteAndHashAsync(identifier, context, fileName, sourceStream, cancellationToken);

        public ValueTask<bool> IsHealthAsync(CancellationToken cancellationToken = default)
            => inner.IsHealthAsync(cancellationToken);

        public void Dispose() => inner.Dispose();

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
