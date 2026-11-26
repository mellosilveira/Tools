using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.Migrator;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.RelationalDatabase.Repositories;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.ResiliencePipelines;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Polly;
using Testcontainers.PostgreSql;

namespace AcceptanceTests.Database;

/// <summary>
/// Integration tests verifying end-to-end database operations and Row-Level Security (RLS)
/// against a live PostgreSQL 17 container using <see cref="MigratorEngine"/>, <see cref="ISqlProvider"/>,
/// and <see cref="PostgresRepository"/>.
/// </summary>
public sealed class PostgresIntegrationTests
{
    private const string FallbackLocalConnectionString = "Host=localhost;Port=5432;Database=tools_db;Username=postgres;Password=postgres;";

    /// <summary>
    /// Executes a complete CRUD lifecycle and verifies multi-tenant Row-Level Security policies
    /// (anonymous public append-only, authenticated private + public read + cross-FK to public dataset,
    /// and DB-backed superuser + instant revocation) against live PostgreSQL using the non-superuser <c>tools_app</c> role.
    /// </summary>
    [PostgresIntegrationFact]
    public async Task Postgres_CrudLifecycleAndRlsPolicies_ShouldEnforceAnonymousPrivateAndSuperuserAccess()
    {
        PostgreSqlContainer? container = null;
        string? adminConnectionString = null;
        Exception? containerException = null;
        Exception? fallbackException = null;

        try
        {
            try
            {
                container = new PostgreSqlBuilder("postgres:17-alpine")
                    .WithDatabase("tools_db")
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .Build();

                await container.StartAsync();
                adminConnectionString = container.GetConnectionString();
            }
            catch (Exception ex)
            {
                containerException = ex;
                container = null;
            }

            if (string.IsNullOrWhiteSpace(adminConnectionString))
            {
                string? envConn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
                    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

                string candidate = !string.IsNullOrWhiteSpace(envConn) ? envConn : FallbackLocalConnectionString;

                try
                {
                    await using NpgsqlConnection testConn = new(candidate);
                    await testConn.OpenAsync();
                    adminConnectionString = candidate;
                }
                catch (Exception ex)
                {
                    fallbackException = ex;
                    adminConnectionString = null;
                }
            }

            if (string.IsNullOrWhiteSpace(adminConnectionString))
            {
                Assert.Fail(
                    $"PostgreSQL integration test could not connect via Testcontainers ({containerException?.GetType().Name}: {containerException?.Message}) " +
                    $"or fallback connection ({fallbackException?.GetType().Name}: {fallbackException?.Message}).");
            }

            // 1. Run clean DbUp migrations as database owner.
            ILogger migratorLogger = Mock.Of<ILogger>();
            MigratorOptions migratorOptions = new(adminConnectionString, MigrationContext.All, 60);
            Result migrationResult = MigratorEngine.UpgradeDatabase(migratorLogger, migratorOptions);
            Assert.True(migrationResult.Success, $"Migration failed: {migrationResult.JoinedMessages}");

            // 2. Connect as non-superuser role 'tools_app' with MaxPoolSize=1 so every sequential call
            //    reuses the exact same physical PostgreSQL connection, proving zero RLS leakage across NpgsqlBatch calls.
            NpgsqlConnectionStringBuilder appConnBuilder = new(adminConnectionString)
            {
                Username = "tools_app",
                Password = "tools_app_pass",
                MaxPoolSize = 1
            };

            ISqlProvider sqlProvider = new PostgresSqlProvider();
            DatabaseSettings databaseSettings = new()
            {
                ConnectionString = appConnBuilder.ConnectionString,
                UnitOperationTimeoutInMilliseconds = 30_000,
                BulkOperationTimeoutInMilliseconds = 60_000
            };
            ResiliencePipelineSettings resilienceSettings = new()
            {
                BackoffType = DelayBackoffType.Constant,
                DelayInMilliseconds = 100,
                MaxRetryAttempts = 3,
                UseJitter = false
            };
            PostgresResiliencePipeline resiliencePipeline = new(Mock.Of<ILogger<PostgresResiliencePipeline>>(), resilienceSettings);
            CurrentUserProvider currentUserProvider = new();
            await using NpgsqlDataSource dataSource = DatabaseDependencyInjection.CreateDataSource(databaseSettings);
            PostgresRepository repository = new(sqlProvider, currentUserProvider, dataSource, resiliencePipeline, databaseSettings);

            // Seed three accounts: admin (superuser), userA, userB.
            string suffix = Guid.NewGuid().ToString("N")[..8];
            long adminId = await repository.InsertAsync(new UserEntity { Username = $"admin_{suffix}", PasswordHash = "hash", IsActive = true, IsSuperuser = true });
            long userAId = await repository.InsertAsync(new UserEntity { Username = $"userA_{suffix}", PasswordHash = "hash", IsActive = true, IsSuperuser = false });
            long userBId = await repository.InsertAsync(new UserEntity { Username = $"userB_{suffix}", PasswordHash = "hash", IsActive = true, IsSuperuser = false });

            // =========================================================================
            // 3. Anonymous user: creates public records (user_id IS NULL), reads public,
            //    handles NULLS NOT DISTINCT TryInsert conflict, and CANNOT update/delete.
            // =========================================================================
            StorageEntryMetadata publicFileMeta = new(StorageProvider.Local, $"samples/{suffix}/public_acl.csv", $"samples/{suffix}", "public_acl.csv", 2048, DateTimeOffset.UtcNow);
            long publicFileId;
            using (currentUserProvider.BeginScope(null))
            {
                publicFileId = await repository.InsertAsync(StorageEntryMetadataEntity.Create(publicFileMeta, $"PUB-{suffix}", "text/csv"));
                StorageEntryMetadataEntity? anonFetched = await repository.GetAsync<StorageEntryMetadataEntity>(publicFileId);
                Assert.NotNull(anonFetched);
                Assert.Null(anonFetched.UserId);

                // Anonymous cannot update or delete public records (append-only RLS).
                bool anonUpdated = await repository.TryUpdateAsync(anonFetched with { SizeBytes = 9999 });
                Assert.False(anonUpdated);
                await repository.DeleteAsync<StorageEntryMetadataEntity>(publicFileId);
                Assert.NotNull(await repository.GetAsync<StorageEntryMetadataEntity>(publicFileId));

                // NULLS NOT DISTINCT uniqueness + IS NOT DISTINCT FROM TryInsertAsync conflict for anonymous!
                MechanicalModelCurveFitOutputEntity publicFit = CreateCurveFit($"FIT-PUB-{suffix}", $"EXT-PUB-{suffix}");
                Result<long> firstAnonFit = await repository.TryInsertAsync(publicFit);
                Assert.True(firstAnonFit.Success);
                Result<long> dupAnonFit = await repository.TryInsertAsync(publicFit);
                Assert.True(dupAnonFit.IsConflict);
                Assert.Equal(firstAnonFit.Data, dupAnonFit.Data);
            }

            // =========================================================================
            // 4. Authenticated User A: creates private records, reads own + public,
            //    creates simulation referencing public storage_entry_metadata (simple FK!),
            //    and CANNOT see/modify User B's records or modify public records.
            // =========================================================================
            long userAFileId;
            long userASimId;
            using (currentUserProvider.BeginScope(userAId))
            {
                StorageEntryMetadata userAFileMeta = new(StorageProvider.Local, $"uA/{suffix}/private.csv", $"uA/{suffix}", "private.csv", 1024, DateTimeOffset.UtcNow);
                userAFileId = await repository.InsertAsync(StorageEntryMetadataEntity.Create(userAFileMeta, $"UA-{suffix}", "text/csv"));

                StorageEntryMetadataEntity? fetchedOwn = await repository.GetAsync<StorageEntryMetadataEntity>(userAFileId);
                Assert.NotNull(fetchedOwn);
                Assert.Equal(userAId, fetchedOwn.UserId);

                // User A can read the public dataset and reference it in a private simulation!
                Assert.NotNull(await repository.GetAsync<StorageEntryMetadataEntity>(publicFileId));
                MechanicalModelSimulationEntity sim = new()
                {
                    Identifier = $"SIM-UA-{suffix}",
                    CurveFitIdentifier = $"FIT-PUB-{suffix}",
                    ExternalIdentifier = $"UA-{suffix}",
                    MechanicalModelName = "Fung",
                    ExperimentalDataFileMetadataId = publicFileId,
                    SimulationOutputFileMetadataId = userAFileId,
                    InitialOutputJson = "{}",
                    FinalOutputJson = "{}",
                    AbsoluteDeltaOutputJson = "{}",
                    PercentageDeltaOutputJson = "{}"
                };
                userASimId = await repository.InsertAsync(sim);
                Assert.True(userASimId > 0);

                // User A can update own file, but CANNOT update public file.
                Assert.True(await repository.TryUpdateAsync(fetchedOwn with { SizeBytes = 4096 }));
                StorageEntryMetadataEntity? publicSeenByA = await repository.GetAsync<StorageEntryMetadataEntity>(publicFileId);
                Assert.False(await repository.TryUpdateAsync(publicSeenByA! with { SizeBytes = 7777 }));

                // Verify single-round-trip NpgsqlBatch reads (Count, Exist, streaming GetAsync with early break)
                // and 3-command atomic bulk UpsertAsync (set_config + DELETE + INSERT) on the single pooled connection.
                Assert.True(await repository.ExistAsync<StorageEntryMetadataEntity>(userAFileId));
                Assert.True(await repository.CountAsync<StorageEntryMetadataEntity, StorageEntryMetadataFilter>(new StorageEntryMetadataFilter { ExternalIdentifier = $"UA-{suffix}" }) >= 1L);

                int streamedCount = 0;
                await foreach (StorageEntryMetadataEntity _ in repository.GetAsync<StorageEntryMetadataEntity, StorageEntryMetadataFilter>(new StorageEntryMetadataFilter()))
                {
                    streamedCount++;
                    break;
                }
                Assert.Equal(1, streamedCount);

                MechanicalModelCurveFitOutputEntity userAFit1 = CreateCurveFit($"FIT-UA-1-{suffix}", $"EXT-UA-{suffix}");
                MechanicalModelCurveFitOutputEntity userAFit2 = CreateCurveFit($"FIT-UA-2-{suffix}", $"EXT-UA-{suffix}");
                long[] upsertedFitIds = await repository.UpsertAsync(
                    [userAFit1, userAFit2],
                    new MechanicalModelCurveFitOutputFilter { ExtenalIdentifier = $"EXT-UA-{suffix}" });
                Assert.Equal(2, upsertedFitIds.Length);
                Assert.Equal(2L, await repository.CountAsync<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>(new MechanicalModelCurveFitOutputFilter { ExtenalIdentifier = $"EXT-UA-{suffix}" }));
            }

            // User B sees public file, CANNOT see User A's private file, simulation, or curve fits
            // (reusing the exact same physical connection with MaxPoolSize=1).
            using (currentUserProvider.BeginScope(userBId))
            {
                Assert.NotNull(await repository.GetAsync<StorageEntryMetadataEntity>(publicFileId));
                Assert.Null(await repository.GetAsync<StorageEntryMetadataEntity>(userAFileId));
                Assert.Null(await repository.GetAsync<MechanicalModelSimulationEntity>(userASimId));
                Assert.Equal(0L, await repository.CountAsync<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>(new MechanicalModelCurveFitOutputFilter { ExtenalIdentifier = $"EXT-UA-{suffix}" }));
            }

            // Anonymous also CANNOT see User A's private records.
            using (currentUserProvider.BeginScope(null))
            {
                Assert.Null(await repository.GetAsync<StorageEntryMetadataEntity>(userAFileId));
                Assert.Null(await repository.GetAsync<MechanicalModelSimulationEntity>(userASimId));
            }

            // =========================================================================
            // 5. Superuser (verified directly in users table by PostgreSQL RLS):
            //    reads/modifies/deletes ALL records, and revoking is_superuser takes effect immediately!
            // =========================================================================
            using (currentUserProvider.BeginScope(adminId, isSuperuser: true))
            {
                StorageEntryMetadataEntity? adminReadPublic = await repository.GetAsync<StorageEntryMetadataEntity>(publicFileId);
                StorageEntryMetadataEntity? adminReadUserA = await repository.GetAsync<StorageEntryMetadataEntity>(userAFileId);
                Assert.NotNull(adminReadPublic);
                Assert.NotNull(adminReadUserA);

                Assert.True(await repository.TryUpdateAsync(adminReadPublic with { SizeBytes = 8192 }));
                Assert.True(await repository.TryUpdateAsync(adminReadUserA with { SizeBytes = 8192 }));

                // Revoke superuser in DB -> next query immediately loses access to User A's private row!
                UserEntity? adminEntity = await repository.GetAsync<UserEntity>(adminId);
                Assert.True(await repository.TryUpdateAsync(adminEntity! with { IsSuperuser = false }));
                Assert.Null(await repository.GetAsync<StorageEntryMetadataEntity>(userAFileId));

                // Restore superuser and clean up all test records.
                Assert.True(await repository.TryUpdateAsync(adminEntity with { IsSuperuser = true }));
                await repository.DeleteAsync<MechanicalModelSimulationEntity>(userASimId);
                await repository.DeleteAsync<StorageEntryMetadataEntity>(userAFileId);
                await repository.DeleteAsync<StorageEntryMetadataEntity>(publicFileId);
                await repository.DeleteAsync<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>(new MechanicalModelCurveFitOutputFilter { ExtenalIdentifier = $"EXT-PUB-{suffix}" });
                await repository.DeleteAsync<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>(new MechanicalModelCurveFitOutputFilter { ExtenalIdentifier = $"EXT-UA-{suffix}" });
            }

            // =========================================================================
            // 6. Model 2 Plugin Security RLS + storage_entry_metadata FK & Strategy 1:
            //    anonymous cannot submit, User A submits Pending (with private storage_entry_metadata FK),
            //    ON DELETE RESTRICT blocks deleting storage_entry_metadata while referenced,
            //    cannot self-approve, author can cancel Pending (child -> parent),
            //    on approval storage_entry_metadata.user_id becomes NULL (public read/download,
            //    superuser-only edit/delete) while author can NEVER modify/delete Approved
            //    submission or its promoted storage_entry_metadata record.
            // =========================================================================
            // Anonymous cannot insert plugin submissions or plugin_cache entries (PostgreSQL RLS 42501).
            using (currentUserProvider.BeginScope(null))
            {
                await Assert.ThrowsAsync<PostgresException>(() => repository.InsertAsync(new PluginSubmissionEntity
                {
                    StorageEntryMetadataId = 1L,
                    PluginName = $"CommunityModel_{suffix}",
                    PluginVersion = "v1.0.0",
                    Sha256Hash = "deadbeef",
                    Status = PluginSubmissionStatus.Pending
                }));
                await Assert.ThrowsAsync<PostgresException>(() => repository.InsertAsync(new PluginCacheEntity
                {
                    PluginName = $"Cache_{suffix}",
                    PluginVersion = "v1.0.0",
                    StateType = "RegisteredPlugin",
                    StateJson = "{}"
                }));
            }

            long pendingFileA1Id;
            long pendingFileA2Id;
            long approvedSubmissionId;
            long cancelableSubmissionId;
            using (currentUserProvider.BeginScope(userAId))
            {
                pendingFileA1Id = await repository.InsertAsync(new StorageEntryMetadataEntity
                {
                    ExternalIdentifier = $"CommunityModel_{suffix}@v1.0.0",
                    StorageProvider = StorageProvider.Local,
                    Location = $"plugins/pending/CommunityModel_{suffix}.v1.0.0.dll",
                    Directory = "plugins/pending",
                    FileName = $"CommunityModel_{suffix}.v1.0.0.dll",
                    SizeBytes = 1024,
                    ContentType = "application/octet-stream"
                });
                pendingFileA2Id = await repository.InsertAsync(new StorageEntryMetadataEntity
                {
                    ExternalIdentifier = $"CommunityModel_{suffix}@v1.0.1",
                    StorageProvider = StorageProvider.Local,
                    Location = $"plugins/pending/CommunityModel_{suffix}.v1.0.1.dll",
                    Directory = "plugins/pending",
                    FileName = $"CommunityModel_{suffix}.v1.0.1.dll",
                    SizeBytes = 1024,
                    ContentType = "application/octet-stream"
                });

                PluginSubmissionEntity userAPending = new()
                {
                    StorageEntryMetadataId = pendingFileA1Id,
                    PluginName = $"CommunityModel_{suffix}",
                    PluginVersion = "v1.0.0",
                    Sha256Hash = "deadbeef",
                    Status = PluginSubmissionStatus.Pending
                };

                // Non-superuser cannot self-approve on insert (status = Approved violates RLS WITH CHECK).
                await Assert.ThrowsAsync<PostgresException>(() => repository.InsertAsync(userAPending with { Status = PluginSubmissionStatus.Approved }));

                approvedSubmissionId = await repository.InsertAsync(userAPending);
                cancelableSubmissionId = await repository.InsertAsync(userAPending with { StorageEntryMetadataId = pendingFileA2Id, PluginVersion = "v1.0.1" });

                Assert.NotNull(await repository.GetAsync<PluginSubmissionEntity>(approvedSubmissionId));

                // ON DELETE RESTRICT: deleting storage_entry_metadata while referenced by plugin_submissions fails (23503).
                PostgresException fkViolation = await Assert.ThrowsAsync<PostgresException>(() => repository.DeleteAsync<StorageEntryMetadataEntity>(pendingFileA2Id));
                Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, fkViolation.SqlState);

                // User A cannot UPDATE own submission to Approved (superuser-only UPDATE RLS).
                PluginSubmissionEntity own = (await repository.GetAsync<PluginSubmissionEntity>(approvedSubmissionId))!;
                Assert.False(await repository.TryUpdateAsync(own with { Status = PluginSubmissionStatus.Approved }));

                // User A CAN cancel/delete own Pending submission (child first, then storage_entry_metadata parent).
                await repository.DeleteAsync<PluginSubmissionEntity>(cancelableSubmissionId);
                await repository.DeleteAsync<StorageEntryMetadataEntity>(pendingFileA2Id);
                Assert.Null(await repository.GetAsync<PluginSubmissionEntity>(cancelableSubmissionId));
                Assert.Null(await repository.GetAsync<StorageEntryMetadataEntity>(pendingFileA2Id));
            }

            // User B CANNOT see or delete User A's Pending submission or its private storage_entry_metadata record.
            using (currentUserProvider.BeginScope(userBId))
            {
                Assert.Null(await repository.GetAsync<PluginSubmissionEntity>(approvedSubmissionId));
                Assert.Null(await repository.GetAsync<StorageEntryMetadataEntity>(pendingFileA1Id));
                await repository.DeleteAsync<PluginSubmissionEntity>(approvedSubmissionId);
            }

            // Superuser approves submission, promotes storage_entry_metadata.user_id = NULL (Strategy 1), and writes global plugin_cache entry.
            long pluginCacheId;
            using (currentUserProvider.BeginScope(adminId, isSuperuser: true))
            {
                StorageEntryMetadataEntity pendingFileRow = (await repository.GetAsync<StorageEntryMetadataEntity>(pendingFileA1Id))!;
                Assert.True(await repository.TryUpdateAsync(pendingFileRow with
                {
                    UserId = null,
                    Location = $"plugins/incoming/CommunityModel_{suffix}.v1.0.0.dll",
                    Directory = "plugins/incoming"
                }));

                PluginSubmissionEntity pendingRow = (await repository.GetAsync<PluginSubmissionEntity>(approvedSubmissionId))!;
                Assert.True(await repository.TryUpdateAsync(pendingRow with
                {
                    Status = PluginSubmissionStatus.Approved,
                    ReviewedByUserId = adminId,
                    ReviewedAt = DateTimeOffset.UtcNow,
                    ReviewNotes = "Verified"
                }));

                pluginCacheId = await repository.InsertAsync(new PluginCacheEntity
                {
                    PluginName = $"CommunityModel_{suffix}",
                    PluginVersion = "v1.0.0",
                    StateType = "RegisteredPlugin",
                    StateJson = "{}"
                });
            }

            // Strategy 1: once Approved, everyone (Anonymous, User A, User B) can read submission, storage_entry_metadata (UserId = null), and plugin_cache;
            // and original author User A can NO LONGER edit or delete either the Approved submission or its promoted storage_entry_metadata entry!
            using (currentUserProvider.BeginScope(null))
            {
                Assert.NotNull(await repository.GetAsync<PluginSubmissionEntity>(approvedSubmissionId));
                StorageEntryMetadataEntity? publicPluginFile = await repository.GetAsync<StorageEntryMetadataEntity>(pendingFileA1Id);
                Assert.NotNull(publicPluginFile);
                Assert.Null(publicPluginFile.UserId);
                Assert.NotNull(await repository.GetAsync<PluginCacheEntity>(pluginCacheId));
            }

            using (currentUserProvider.BeginScope(userBId))
            {
                StorageEntryMetadataEntity? userBPluginFile = await repository.GetAsync<StorageEntryMetadataEntity>(pendingFileA1Id);
                Assert.NotNull(userBPluginFile);
                Assert.Null(userBPluginFile.UserId);
            }

            using (currentUserProvider.BeginScope(userAId))
            {
                StorageEntryMetadataEntity? promotedFileAsAuthor = await repository.GetAsync<StorageEntryMetadataEntity>(pendingFileA1Id);
                Assert.NotNull(promotedFileAsAuthor);
                Assert.Null(promotedFileAsAuthor.UserId);
                Assert.False(await repository.TryUpdateAsync(promotedFileAsAuthor with { SizeBytes = 999999 }));

                await repository.DeleteAsync<PluginSubmissionEntity>(approvedSubmissionId);
                await repository.DeleteAsync<StorageEntryMetadataEntity>(pendingFileA1Id);
                Assert.NotNull(await repository.GetAsync<PluginSubmissionEntity>(approvedSubmissionId));
                Assert.NotNull(await repository.GetAsync<StorageEntryMetadataEntity>(pendingFileA1Id));
            }

            using (currentUserProvider.BeginScope(adminId, isSuperuser: true))
            {
                await repository.DeleteAsync<PluginSubmissionEntity>(approvedSubmissionId);
                await repository.DeleteAsync<StorageEntryMetadataEntity>(pendingFileA1Id);
                await repository.DeleteAsync<PluginCacheEntity>(pluginCacheId);
            }
        }
        finally
        {
            if (container is not null)
                await container.DisposeAsync();
        }
    }

    private static MechanicalModelCurveFitOutputEntity CreateCurveFit(string identifier, string externalIdentifier) => new()
    {
        Identifier = identifier,
        ExtenalIdentifier = externalIdentifier,
        MechanicalModelName = "Fung",
        InitialAcceptedRange = 0.0,
        FinalAcceptedRange = 12.5,
        LoadResponseRelationship = LoadResponseRelationship.StressStrain,
        RampTimeConsideration = RampTimeConsideration.ConsiderWithViscoelasticEffect,
        ViscoelasticEffect = ViscoelasticEffect.Relaxation,
        Error = 0.00035,
        Iterations = 28,
        ConstitutiveParameters = "{\"c\": 1.25, \"A\": 0.85, \"B\": 12.4}"
    };

    private sealed class PostgresIntegrationFactAttribute : FactAttribute
    {
        public PostgresIntegrationFactAttribute()
        {
            bool requireInCi = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Environment.GetEnvironmentVariable("REQUIRE_POSTGRES_TESTS"), "true", StringComparison.OrdinalIgnoreCase);

            if (requireInCi)
                return;

            bool hasConfiguredConnection = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING"))
                || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CONNECTION_STRING"));

            if (hasConfiguredConnection || IsDockerDaemonAvailable())
                return;

            Skip = "Docker daemon is not running and POSTGRES_CONNECTION_STRING is not set (skipped locally; enforced in CI).";
        }

        private static bool IsDockerDaemonAvailable()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    string[] pipes = Directory.GetFiles(@"\\.\pipe\", "docker_engine");
                    return pipes.Length > 0;
                }

                return File.Exists("/var/run/docker.sock");
            }
            catch
            {
                return false;
            }
        }
    }
}
