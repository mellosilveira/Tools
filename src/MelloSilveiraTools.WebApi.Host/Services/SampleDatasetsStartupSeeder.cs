using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Create;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Read;
using MelloSilveiraTools.WebApi.Host.Commands;

namespace MelloSilveiraTools.WebApi.Host.Services;

/// <summary>
/// Hosted service that bootstraps the configured superuser account on startup and,
/// when <see cref="Settings.UploadSampleDatasetsOnStartup"/> is enabled, uploads bundled CSV files
/// from <c>SamplesData/</c> as public storage entries (<c>user_id IS NULL</c>) under the superuser context.
/// </summary>
/// <param name="logger">Structured logger for startup seeding diagnostics.</param>
/// <param name="encryptionService">Scope factory used to resolve scoped services such as <see cref="IEncryptionService"/>.</param>
/// <param name="repository">Relational repository used to ensure the superuser account exists.</param>
/// <param name="currentUserProvider">User context provider used to establish the superuser RLS scope during sample upload.</param>
/// <param name="addStorageEntryCommand">Command used to store each sample file and persist its metadata.</param>
/// <param name="readStorageEntriesCommand">Command used to check for already-uploaded sample files for idempotency.</param>
/// <param name="settings">Host settings carrying superuser credentials and sample-seeding flags.</param>
public sealed class SampleDatasetsStartupSeeder(
    ILogger<SampleDatasetsStartupSeeder> logger,
    IRepository repository,
    ICurrentUserProvider currentUserProvider,
    IEncryptionService encryptionService,
    AddStorageEntryCommand addStorageEntryCommand,
    ReadStorageEntriesCommand readStorageEntriesCommand,
    Settings settings) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            long superuserId = await EnsureSuperuserAsync(cancellationToken).ConfigureAwait(false);

            if (!settings.UploadSampleDatasetsOnStartup)
                return;

            using IDisposable _ = currentUserProvider.BeginScope(superuserId, isSuperuser: true);
            await SeedPublicSampleDatasetsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Skipping superuser/sample startup seeding because storage or database is unavailable.");
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<long> EnsureSuperuserAsync(CancellationToken cancellationToken)
    {
        string username = settings.Superuser.Username.Trim();
        UserEntity? existing = await repository
            .GetFirstOrDefaultAsync<UserEntity, UserFilter>(new UserFilter { Username = username }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            bool passwordMatches = encryptionService.IsPasswordValid(settings.Superuser.Password, existing.PasswordHash);
            if (!existing.IsSuperuser || !existing.IsActive || !passwordMatches)
            {
                UserEntity updated = existing with
                {
                    PasswordHash = passwordMatches ? existing.PasswordHash : encryptionService.GeneratePasswordHash(settings.Superuser.Password),
                    IsSuperuser = true,
                    IsActive = true
                };
                await repository.TryUpdateAsync(updated, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Synchronized configured superuser account '{Username}' (Id={SuperuserId}).", username, existing.Id);
            }

            return existing.Id;
        }

        UserEntity superuser = new()
        {
            Username = username,
            PasswordHash = encryptionService.GeneratePasswordHash(settings.Superuser.Password),
            IsActive = true,
            IsSuperuser = true
        };

        long superuserId = await repository.InsertAsync(superuser, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Bootstrapped superuser account '{Username}' (Id={SuperuserId}).", username, superuserId);
        return superuserId;
    }

    private async Task SeedPublicSampleDatasetsAsync(CancellationToken cancellationToken)
    {
        string samplesDir = Path.Combine(AppContext.BaseDirectory, GetSampleDatasetsCommand.SampleContext);
        if (!Directory.Exists(samplesDir))
        {
            logger.LogWarning("Sample datasets directory '{SamplesDir}' was not found; skipping startup upload.", samplesDir);
            return;
        }

        string[] csvFiles = Directory.GetFiles(samplesDir, "*.csv", SearchOption.TopDirectoryOnly);
        if (csvFiles.Length == 0)
            return;

        ListedResult<StorageEntryResultData> existingResult = await readStorageEntriesCommand
            .ExecuteAsync(new ReadStorageEntriesRequest
            {
                Identifier = GetSampleDatasetsCommand.SampleIdentifier,
                Context = GetSampleDatasetsCommand.SampleContext
            }, cancellationToken)
            .ConfigureAwait(false);

        HashSet<string> existingFileNames = existingResult.Success && existingResult.Data is not null
            ? new HashSet<string>(existingResult.Data.Select(entry => entry.Metadata.FileName).Where(name => !string.IsNullOrWhiteSpace(name))!, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string filePath in csvFiles)
        {
            string fileName = Path.GetFileName(filePath);
            if (existingFileNames.Contains(fileName))
                continue;

            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            FormFile formFile = new(fileStream, 0, fileStream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };

            Result<StorageEntryResultData> uploadResult = await addStorageEntryCommand
                .ExecuteAsync(new AddStorageEntryRequest
                {
                    File = formFile,
                    Identifier = GetSampleDatasetsCommand.SampleIdentifier,
                    Context = GetSampleDatasetsCommand.SampleContext,
                    ExternalIdentifier = GetSampleDatasetsCommand.SampleIdentifier
                }, cancellationToken)
                .ConfigureAwait(false);

            if (!uploadResult.Success)
            {
                logger.LogWarning("Failed to upload sample dataset '{FileName}' on startup: {Messages}", fileName, uploadResult.JoinedMessages);
            }
        }
    }
}
