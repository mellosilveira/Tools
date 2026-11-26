using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.Migrator;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.RelationalDatabase.Repositories;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Register;
using MelloSilveiraTools.WebApi.Application.Middlewares;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using Npgsql;
using Polly;
using System.Reflection;
using System.Security.Claims;

namespace UnitTests.Database;

/// <summary>
/// Exhaustive unit tests covering PostgreSQL Row-Level Security (RLS) without partitioning,
/// NULL-safe <c>IS NOT DISTINCT FROM</c> SQL generation, instance-scoped <see cref="CurrentUserProvider"/>
/// concurrency/isolation/idempotency, DB-backed <see cref="UserContextMiddleware"/> active/superuser resolution
/// and instant revocation, DI registration, user registration, and migration script invariants.
/// </summary>
public sealed class RowLevelSecurityAndPartitioningTests
{
    private readonly ISqlProvider _sqlProvider = new PostgresSqlProvider();

    [Fact]
    public void PostgresSqlProvider_UserScopedEntitiesWithUniqueColumn_UseNullSafeCompositeIdentifierAndUserIdConflictKey()
    {
        string curveFitInsertSql = _sqlProvider.GetInsertSql<MechanicalModelCurveFitOutputEntity>();
        Assert.Contains("user_id", curveFitInsertSql);
        Assert.Contains("@UserId", curveFitInsertSql);
        Assert.Contains("ON CONFLICT (identifier, user_id)", curveFitInsertSql);
        Assert.DoesNotContain("user_id = EXCLUDED.user_id", curveFitInsertSql);

        string curveFitTryInsertSql = _sqlProvider.GetTryInsertSql<MechanicalModelCurveFitOutputEntity>();
        Assert.Contains("ON CONFLICT (identifier, user_id) DO NOTHING", curveFitTryInsertSql);
        Assert.Contains("WHERE identifier IS NOT DISTINCT FROM @Identifier AND user_id IS NOT DISTINCT FROM @UserId", curveFitTryInsertSql);

        string simulationBulkSql = _sqlProvider.GetBulkInsertSql<MechanicalModelSimulationEntity>(2);
        Assert.Contains("@UserId_1", simulationBulkSql);
        Assert.Contains("@UserId_2", simulationBulkSql);
        Assert.Contains("ON CONFLICT (identifier, user_id)", simulationBulkSql);

        string sweepInsertSql = _sqlProvider.GetInsertSql<ParametricSweepEntity>();
        Assert.Contains("ON CONFLICT (identifier, user_id)", sweepInsertSql);
        Assert.DoesNotContain("user_id = EXCLUDED.user_id", sweepInsertSql);

        string byUniqueSql = _sqlProvider.GetSelectByUniqueColumnSql<MechanicalModelCurveFitOutputEntity>();
        Assert.Contains("identifier = @UniqueColumnValue", byUniqueSql);
    }

    [Fact]
    public void PostgresSqlProvider_StorageEntryMetadataEntity_IncludesUserIdWithoutOnConflict()
    {
        string insertSql = _sqlProvider.GetInsertSql<StorageEntryMetadataEntity>();
        Assert.Contains("INSERT INTO storage_entry_metadata", insertSql);
        Assert.Contains("user_id", insertSql);
        Assert.Contains("@UserId", insertSql);
        Assert.DoesNotContain("ON CONFLICT", insertSql);
    }

    [Fact]
    public void BuildParameters_UseDeclaredPropertiesTrue_IncludesUserIdForUserScopedEntitiesOnly()
    {
        StorageEntryMetadata metadata = new(StorageProvider.Local, "loc/file.csv", "loc", "file.csv", 128, DateTimeOffset.UtcNow);
        StorageEntryMetadataEntity scopedEntity = StorageEntryMetadataEntity.Create(metadata, "EXP-1") with { UserId = 42L };

        List<NpgsqlParameter> scopedParams = [.. scopedEntity.BuildParameters(useDeclaredProperties: true)];
        NpgsqlParameter? userIdParam = scopedParams.FirstOrDefault(p => p.ParameterName == nameof(UserScopedEntityBase.UserId));
        Assert.NotNull(userIdParam);
        Assert.Equal(42L, userIdParam.Value);
        Assert.DoesNotContain(scopedParams, p => p.ParameterName == nameof(EntityBase.Id));

        StorageEntryMetadataEntity publicEntity = StorageEntryMetadataEntity.Create(metadata, "PUB-1");
        List<NpgsqlParameter> publicParams = [.. publicEntity.BuildParameters(useDeclaredProperties: true)];
        NpgsqlParameter? publicUserIdParam = publicParams.FirstOrDefault(p => p.ParameterName == nameof(UserScopedEntityBase.UserId));
        Assert.NotNull(publicUserIdParam);
        Assert.Equal(DBNull.Value, publicUserIdParam.Value);

        ProductEntity globalEntity = new() { Id = 1L, Name = "Global", Price = 10L };
        List<NpgsqlParameter> globalParams = [.. globalEntity.BuildParameters(useDeclaredProperties: true)];
        Assert.DoesNotContain(globalParams, p => p.ParameterName == nameof(UserScopedEntityBase.UserId));
    }

    [Fact]
    public async Task CurrentUserProvider_NestedScopesIncludingNullAndInstanceIsolation_WorkCorrectly()
    {
        CurrentUserProvider provider = new();
        CurrentUserProvider isolatedProvider = new();
        Assert.Null(provider.GetCurrentUserId());
        Assert.Null(isolatedProvider.GetCurrentUserId());

        using (provider.BeginScope(77L))
        {
            Assert.Equal(77L, provider.GetCurrentUserId());
            Assert.Null(isolatedProvider.GetCurrentUserId());

            long? backgroundObservedUserId = await Task.Run(() => provider.GetCurrentUserId());
            Assert.Equal(77L, backgroundObservedUserId);

            using (provider.BeginScope(99L))
            {
                Assert.Equal(99L, provider.GetCurrentUserId());

                using (provider.BeginScope(null))
                {
                    Assert.Null(provider.GetCurrentUserId());
                }

                Assert.Equal(99L, provider.GetCurrentUserId());
            }

            Assert.Equal(77L, provider.GetCurrentUserId());
        }

        Assert.Null(provider.GetCurrentUserId());
    }

    [Fact]
    public void CurrentUserProvider_DoubleDisposeOnScopeHandle_IsIdempotentAndDoesNotCorruptNewScope()
    {
        CurrentUserProvider provider = new();

        IDisposable firstHandle = provider.BeginScope(10L);
        Assert.Equal(10L, provider.GetCurrentUserId());

        firstHandle.Dispose();
        Assert.Null(provider.GetCurrentUserId());

        using (provider.BeginScope(20L))
        {
            Assert.Equal(20L, provider.GetCurrentUserId());

            firstHandle.Dispose();
            Assert.Equal(20L, provider.GetCurrentUserId());
        }

        Assert.Null(provider.GetCurrentUserId());
    }

    [Fact]
    public async Task UserContextMiddleware_ConcurrentUsersAndBackgroundTaskAfterRequestDispose_HaveZeroCollision()
    {
        CurrentUserProvider sharedSingletonProvider = new();
        IRepository repository = CreateActiveUsersRepository();

        TaskCompletionSource reqAStartedBg = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource reqBEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource reqAFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseReqB = new(TaskCreationOptions.RunContinuationsAsynchronously);

        long? reqAObservedBeforeMiddleware = null;
        long? reqBObservedBeforeMiddleware = null;
        long? reqAObservedInside = null;
        long? reqBObservedInsideBeforeReqADispose = null;
        long? reqBObservedInsideAfterReqADispose = null;
        long? reqAObservedAfterDispose = null;
        long? reqBObservedAfterDispose = null;
        Task<long?>? reqABackgroundTask = null;

        UserContextMiddleware middlewareA = new(async _ =>
        {
            reqAObservedInside = sharedSingletonProvider.GetCurrentUserId();

            reqABackgroundTask = Task.Run(async () =>
            {
                await reqAFinished.Task;
                await reqBEntered.Task;
                return sharedSingletonProvider.GetCurrentUserId();
            });

            reqAStartedBg.SetResult();
            await reqBEntered.Task;
        });

        UserContextMiddleware middlewareB = new(async _ =>
        {
            reqBObservedInsideBeforeReqADispose = sharedSingletonProvider.GetCurrentUserId();
            reqBEntered.SetResult();

            await reqAFinished.Task;
            reqBObservedInsideAfterReqADispose = sharedSingletonProvider.GetCurrentUserId();

            await releaseReqB.Task;
        });

        Task requestA = Task.Run(async () =>
        {
            reqAObservedBeforeMiddleware = sharedSingletonProvider.GetCurrentUserId();
            DefaultHttpContext ctxA = CreateAuthenticatedContext(JwtRegisteredClaimNames.Sub, "10");
            await middlewareA.InvokeAsync(ctxA, sharedSingletonProvider, repository);
            reqAObservedAfterDispose = sharedSingletonProvider.GetCurrentUserId();
            reqAFinished.SetResult();
        });

        Task requestB = Task.Run(async () =>
        {
            await reqAStartedBg.Task;
            reqBObservedBeforeMiddleware = sharedSingletonProvider.GetCurrentUserId();
            DefaultHttpContext ctxB = CreateAuthenticatedContext(JwtRegisteredClaimNames.Sub, "20");
            await middlewareB.InvokeAsync(ctxB, sharedSingletonProvider, repository);
            reqBObservedAfterDispose = sharedSingletonProvider.GetCurrentUserId();
        });

        await requestA;
        Assert.NotNull(reqABackgroundTask);
        long? bgObservedUserId = await reqABackgroundTask;

        releaseReqB.SetResult();
        await requestB;

        Assert.Null(reqAObservedBeforeMiddleware);
        Assert.Null(reqBObservedBeforeMiddleware);
        Assert.Equal(10L, reqAObservedInside);
        Assert.Equal(20L, reqBObservedInsideBeforeReqADispose);
        Assert.Equal(20L, reqBObservedInsideAfterReqADispose);
        Assert.Equal(10L, bgObservedUserId);
        Assert.Null(reqAObservedAfterDispose);
        Assert.Null(reqBObservedAfterDispose);
        Assert.Null(sharedSingletonProvider.GetCurrentUserId());
    }

    [Fact]
    public async Task UserContextMiddleware_ManyParallelRequestsWithBackgroundTasks_MaintainStrictUserIsolation()
    {
        CurrentUserProvider sharedSingletonProvider = new();
        IRepository repository = CreateActiveUsersRepository();
        const int userCount = 20;

        Task<(long Expected, long? Inside, long? Background, long? AfterDispose)>[] tasks = new Task<(long, long?, long?, long?)>[userCount];
        for (int i = 0; i < userCount; i++)
        {
            long expectedUserId = i + 1L;
            tasks[i] = Task.Run(async () =>
            {
                Assert.Null(sharedSingletonProvider.GetCurrentUserId());

                long? observedInside = null;
                Task<long?>? backgroundTask = null;

                UserContextMiddleware middleware = new(async _ =>
                {
                    await Task.Yield();
                    observedInside = sharedSingletonProvider.GetCurrentUserId();

                    backgroundTask = Task.Run(async () =>
                    {
                        await Task.Yield();
                        return sharedSingletonProvider.GetCurrentUserId();
                    });
                });

                DefaultHttpContext context = CreateAuthenticatedContext(JwtRegisteredClaimNames.Sub, expectedUserId.ToString());
                await middleware.InvokeAsync(context, sharedSingletonProvider, repository);
                long? afterDispose = sharedSingletonProvider.GetCurrentUserId();
                long? observedBg = await backgroundTask!;

                return (expectedUserId, observedInside, observedBg, afterDispose);
            });
        }

        (long Expected, long? Inside, long? Background, long? AfterDispose)[] results = await Task.WhenAll(tasks);
        foreach ((long expected, long? inside, long? background, long? afterDispose) in results)
        {
            Assert.Equal(expected, inside);
            Assert.Equal(expected, background);
            Assert.Null(afterDispose);
        }
    }

    [Fact]
    public async Task UserContextMiddleware_WhenNextMiddlewareThrows_RestoresPreviousScope()
    {
        CurrentUserProvider provider = new();
        IRepository repository = CreateActiveUsersRepository();
        DefaultHttpContext httpContext = CreateAuthenticatedContext(JwtRegisteredClaimNames.Sub, "555");

        UserContextMiddleware middleware = new(_ =>
        {
            Assert.Equal(555L, provider.GetCurrentUserId());
            throw new InvalidOperationException("Simulated downstream failure.");
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(httpContext, provider, repository));
        Assert.Null(provider.GetCurrentUserId());
    }

    [Fact]
    public async Task UserContextMiddleware_RejectsDeactivatedOrDeletedUserImmediatelyWith401()
    {
        CurrentUserProvider provider = new();
        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.GetAsync<UserEntity>(50L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserEntity { Id = 50L, Username = "revoked", PasswordHash = "h", IsActive = false, IsSuperuser = true });
        repoMock
            .Setup(r => r.GetAsync<UserEntity>(51L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        bool nextInvoked = false;
        UserContextMiddleware middleware = new(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext inactiveCtx = CreateAuthenticatedContext(JwtRegisteredClaimNames.Sub, "50");
        await middleware.InvokeAsync(inactiveCtx, provider, repoMock.Object);
        Assert.False(nextInvoked);
        Assert.Equal(StatusCodes.Status401Unauthorized, inactiveCtx.Response.StatusCode);
        Assert.False(inactiveCtx.User.Identity?.IsAuthenticated == true);

        DefaultHttpContext deletedCtx = CreateAuthenticatedContext(JwtRegisteredClaimNames.Sub, "51");
        await middleware.InvokeAsync(deletedCtx, provider, repoMock.Object);
        Assert.False(nextInvoked);
        Assert.Equal(StatusCodes.Status401Unauthorized, deletedCtx.Response.StatusCode);
    }

    [Theory]
    [InlineData(JwtRegisteredClaimNames.Sub, "12345", true, 12345L)]
    [InlineData(ClaimTypes.NameIdentifier, "98765", true, 98765L)]
    [InlineData(JwtRegisteredClaimNames.Sub, "", false, 0L)]
    [InlineData(JwtRegisteredClaimNames.Sub, "   ", false, 0L)]
    [InlineData(JwtRegisteredClaimNames.Sub, "not-a-number", false, 0L)]
    [InlineData(JwtRegisteredClaimNames.Sub, "0", false, 0L)]
    [InlineData(JwtRegisteredClaimNames.Sub, "-10", false, 0L)]
    public void UserContextMiddleware_TryExtractUserId_HandlesValidAndInvalidClaims(
        string claimType,
        string claimValue,
        bool expectedSuccess,
        long expectedUserId)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim(claimType, claimValue)], authenticationType: "Bearer"));
        UserContextMiddleware middleware = new(_ => Task.CompletedTask);

        bool success = middleware.TryExtractUserId(principal, out long actualUserId);

        Assert.Equal(expectedSuccess, success);
        if (expectedSuccess)
            Assert.Equal(expectedUserId, actualUserId);
    }

    [Fact]
    public async Task UserContextMiddleware_TryExtractUserId_CoversUnauthenticatedAnonymousZeroDbHitAndClaimPrecedence()
    {
        CurrentUserProvider provider = new();
        Mock<IRepository> repoMock = new();
        long? observedForUnauthenticated = -1L;
        UserContextMiddleware middleware = new(_ =>
        {
            observedForUnauthenticated = provider.GetCurrentUserId();
            return Task.CompletedTask;
        });

        Assert.False(middleware.TryExtractUserId(null, out _));

        ClaimsPrincipal unauthenticated = new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, "99")]));
        Assert.False(middleware.TryExtractUserId(unauthenticated, out _));

        await middleware.InvokeAsync(new DefaultHttpContext { User = unauthenticated }, provider, repoMock.Object);
        Assert.Null(observedForUnauthenticated);
        repoMock.Verify(r => r.GetAsync<UserEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);

        ClaimsPrincipal bothClaims = new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "222"),
            new Claim(JwtRegisteredClaimNames.Sub, "111")
        ], authenticationType: "Bearer"));
        Assert.True(middleware.TryExtractUserId(bothClaims, out long resolvedId));
        Assert.Equal(111L, resolvedId);
    }

    [Fact]
    public void DependencyInjection_RegistersSingleSharedCurrentUserProviderAndAllowsCustomOverride()
    {
        ResiliencePipelineSettings resilience = new()
        {
            BackoffType = DelayBackoffType.Constant,
            DelayInMilliseconds = 1,
            MaxRetryAttempts = 1,
            UseJitter = false
        };
        DatabaseSettings dbSettings = new()
        {
            ConnectionString = "Host=localhost;Database=tools_db;Username=tools_app;Password=tools_app_pass",
            UnitOperationTimeoutInMilliseconds = 5000,
            BulkOperationTimeoutInMilliseconds = 10000
        };
        JwtSettings jwtSettings = new()
        {
            Issuer = "issuer",
            Audience = "audience",
            EncryptionKey = "12345678901234567890123456789012",
            SigningKey = "12345678901234567890123456789012",
            ClockSkewInSeconds = 30,
            SecurityKeyType = SecurityKeyType.Symmetric,
            TokenExperitationTimeInMinutes = 60
        };

        ServiceCollection defaultServices = new();
        defaultServices.AddCoreServices(resilience);
        defaultServices.AddDatabaseServices(resilience, dbSettings);
        defaultServices.AddJweAuthentication(jwtSettings);

        using ServiceProvider defaultProvider = defaultServices.BuildServiceProvider();
        ICurrentUserProvider userProvider1 = defaultProvider.GetRequiredService<ICurrentUserProvider>();
        ICurrentUserProvider userProvider2 = defaultProvider.GetRequiredService<ICurrentUserProvider>();
        IRepository repository = defaultProvider.GetRequiredService<IRepository>();

        Assert.IsType<CurrentUserProvider>(userProvider1);
        Assert.Same(userProvider1, userProvider2);
        Assert.IsType<PostgresRepository>(repository);

        Mock<ICurrentUserProvider> customMock = new();
        ServiceCollection customServices = new();
        customServices.AddCoreServices(resilience);
        customServices.AddDatabaseServices(resilience, dbSettings);
        customServices.AddSingleton(customMock.Object);
        customServices.AddJweAuthentication(jwtSettings);

        using ServiceProvider customProvider = customServices.BuildServiceProvider();
        Assert.Same(customMock.Object, customProvider.GetRequiredService<ICurrentUserProvider>());
    }

    [Fact]
    public async Task RegisterUserCommand_IssuesTokenOnlyAfterSuccessfulUserInsert()
    {
        Mock<IRepository> repoSuccessMock = new();
        repoSuccessMock
            .Setup(r => r.GetFirstOrDefaultAsync<UserEntity, UserFilter>(It.IsAny<UserFilter>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);
        repoSuccessMock
            .Setup(r => r.TryInsertAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(42L));

        Mock<IEncryptionService> encryptionMock = new();
        encryptionMock.Setup(e => e.GeneratePasswordHash("secret123")).Returns("hashed-secret");

        AuthenticationToken expectedToken = new("jwe-token-value", DateTimeOffset.UtcNow.AddHours(1));
        Mock<IAuthenticationTokenService> tokenMock = new();
        tokenMock.Setup(t => t.Generate(42L)).Returns(expectedToken);

        RegisterUserCommand successCommand = new(
            new RegisterUserValidator(),
            repoSuccessMock.Object,
            encryptionMock.Object,
            tokenMock.Object);

        Result<AuthenticationToken> successResult = await successCommand.ExecuteAsync(new RegisterUserRequest
        {
            Username = "researcher",
            Password = "secret123"
        });

        Assert.True(successResult.Success);
        Assert.Equal("jwe-token-value", successResult.Data.Value);
        tokenMock.Verify(t => t.Generate(42L), Times.Once);

        Mock<IRepository> repoConflictMock = new();
        repoConflictMock
            .Setup(r => r.GetFirstOrDefaultAsync<UserEntity, UserFilter>(It.IsAny<UserFilter>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserEntity { Id = 7L, Username = "researcher", PasswordHash = "h" });

        RegisterUserCommand conflictCommand = new(
            new RegisterUserValidator(),
            repoConflictMock.Object,
            encryptionMock.Object,
            tokenMock.Object);

        Result<AuthenticationToken> conflictResult = await conflictCommand.ExecuteAsync(new RegisterUserRequest
        {
            Username = "researcher",
            Password = "secret123"
        });

        Assert.False(conflictResult.Success);
        Assert.True(conflictResult.IsConflict);

        Mock<IRepository> repoFailureMock = new();
        repoFailureMock
            .Setup(r => r.GetFirstOrDefaultAsync<UserEntity, UserFilter>(It.IsAny<UserFilter>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);
        repoFailureMock
            .Setup(r => r.TryInsertAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateUnknownError<Result<long>>("Insert failed"));

        RegisterUserCommand failureCommand = new(
            new RegisterUserValidator(),
            repoFailureMock.Object,
            encryptionMock.Object,
            tokenMock.Object);

        Result<AuthenticationToken> failureResult = await failureCommand.ExecuteAsync(new RegisterUserRequest
        {
            Username = "new_researcher",
            Password = "secret123"
        });

        Assert.False(failureResult.Success);
    }

    [Fact]
    public void MigrationScripts_EnforceNonPartitionedNullableUserIdRowLevelSecurity_AndDbBackedHelpers()
    {
        Assembly migratorAssembly = typeof(MigratorEngine).Assembly;

        string usersSql = ReadEmbeddedScript(migratorAssembly, "0001_create_users.sql");
        Assert.Contains("CREATE ROLE tools_app", usersSql);
        Assert.Contains("NOBYPASSRLS", usersSql);
        Assert.Contains("is_superuser BOOLEAN NOT NULL DEFAULT FALSE", usersSql);
        Assert.Contains("public.current_app_user_id()", usersSql);
        Assert.Contains("public.current_app_is_superuser()", usersSql);
        Assert.Contains("FROM public.users u", usersSql);
        Assert.Contains("u.is_active = TRUE", usersSql);
        Assert.Contains("u.is_superuser = TRUE", usersSql);
        Assert.DoesNotContain("ensure_user_partitions", usersSql);
        Assert.DoesNotContain("app.is_superuser", usersSql);

        string[] rlsScripts =
        [
            "0002_create_storage_entry_metadata.sql",
            "0001_create_mechanical_model_curve_fit_outputs.sql",
            "0002_create_mechanical_model_simulations.sql",
            "0003_create_parametric_sweeps.sql"
        ];

        foreach (string scriptSuffix in rlsScripts)
        {
            string sql = ReadEmbeddedScript(migratorAssembly, scriptSuffix);
            Assert.DoesNotContain("PARTITION BY", sql);
            Assert.Contains("id BIGSERIAL PRIMARY KEY", sql);
            Assert.Contains("user_id BIGINT NULL REFERENCES users (id)", sql);
            Assert.Contains("ENABLE ROW LEVEL SECURITY", sql);
            Assert.Contains("FORCE ROW LEVEL SECURITY", sql);
            Assert.Contains("FOR SELECT", sql);
            Assert.Contains("FOR INSERT", sql);
            Assert.Contains("FOR UPDATE", sql);
            Assert.Contains("FOR DELETE", sql);
            Assert.Contains("user_id IS NULL", sql);
            Assert.Contains("user_id IS NOT DISTINCT FROM (SELECT public.current_app_user_id())", sql);
            Assert.Contains("(SELECT public.current_app_is_superuser())", sql);
            Assert.Contains("TO tools_app", sql);
        }

        string simulationsSql = ReadEmbeddedScript(migratorAssembly, "0002_create_mechanical_model_simulations.sql");
        Assert.Contains("FOREIGN KEY (experimental_data_file_metadata_id) REFERENCES storage_entry_metadata (id)", simulationsSql);
        Assert.Contains("FOREIGN KEY (simulation_output_file_metadata_id) REFERENCES storage_entry_metadata (id)", simulationsSql);
        Assert.Contains("UNIQUE NULLS NOT DISTINCT (identifier, user_id)", simulationsSql);

        string pluginCacheSql = ReadEmbeddedScript(migratorAssembly, "0001_create_plugin_cache.sql");
        Assert.DoesNotContain("PARTITION BY", pluginCacheSql);
        Assert.Contains("ENABLE ROW LEVEL SECURITY", pluginCacheSql);
        Assert.Contains("FORCE ROW LEVEL SECURITY", pluginCacheSql);
        Assert.Contains("FOR SELECT", pluginCacheSql);
        Assert.Contains("(SELECT public.current_app_is_superuser())", pluginCacheSql);
        Assert.Contains("GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE plugin_cache TO tools_app;", pluginCacheSql);

        string pluginSubmissionsSql = ReadEmbeddedScript(migratorAssembly, "0002_create_plugin_submissions.sql");
        Assert.DoesNotContain("PARTITION BY", pluginSubmissionsSql);
        Assert.Contains("storage_entry_metadata_id BIGINT NOT NULL REFERENCES storage_entry_metadata (id) ON DELETE RESTRICT", pluginSubmissionsSql);
        Assert.DoesNotContain("file_name TEXT", pluginSubmissionsSql);
        Assert.DoesNotContain("storage_location TEXT", pluginSubmissionsSql);
        Assert.DoesNotContain("size_bytes BIGINT", pluginSubmissionsSql);
        Assert.Contains("ENABLE ROW LEVEL SECURITY", pluginSubmissionsSql);
        Assert.Contains("FORCE ROW LEVEL SECURITY", pluginSubmissionsSql);
        Assert.Contains("status = 1", pluginSubmissionsSql);
        Assert.Contains("status = 0", pluginSubmissionsSql);
        Assert.Contains("(SELECT public.current_app_user_id())", pluginSubmissionsSql);
        Assert.Contains("(SELECT public.current_app_is_superuser())", pluginSubmissionsSql);
        Assert.Contains("GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE plugin_submissions TO tools_app;", pluginSubmissionsSql);
    }

    private static IRepository CreateActiveUsersRepository()
    {
        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.GetAsync<UserEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => new UserEntity
            {
                Id = id,
                Username = $"user_{id}",
                PasswordHash = "hash",
                IsActive = true,
                IsSuperuser = false
            });
        return repoMock.Object;
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string claimType, string claimValue) => new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(claimType, claimValue)], authenticationType: "Bearer"))
    };

    private static string ReadEmbeddedScript(Assembly assembly, string suffix)
    {
        string? resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(resourceName);

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
