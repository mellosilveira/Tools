using Amazon.S3;
using Azure.Storage.Blobs;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.AwsS3;
using MelloSilveiraTools.Core.Files.Providers.BlobStorage;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Create;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Upload;
using MelloSilveiraTools.WebApi.Application.Commands.Users.RefreshToken;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Threading.Channels;
using HostSettings = MelloSilveiraTools.WebApi.Host.Settings;

namespace UnitTests.Security;

public sealed class MultiTenantSecurityHardeningTests
{
    [Theory]
    [InlineData("../u_99", "raw", "strain.csv")]
    [InlineData("exp-1", "../../other", "strain.csv")]
    [InlineData("exp-1", "raw", "../evil.csv")]
    public async Task AddStorageEntryValidator_RejectsPathTraversalInIdentifierContextOrFileName(string identifier, string context, string fileName)
    {
        AddStorageEntryValidator validator = new();
        await using MemoryStream content = new("data"u8.ToArray());
        FormFile formFile = new(content, 0, content.Length, "file", fileName);

        Result validation = await validator.ValidateAsync(new AddStorageEntryRequest
        {
            File = formFile,
            Identifier = identifier,
            Context = context
        });

        Assert.False(validation.Success);
        Assert.Equal(StatusCode.BadRequest, validation.StatusCode);
    }

    [Fact]
    public async Task DeleteStorageEntryCommand_BlocksCrossUserRawLocationDeletion_ForNonSuperuser()
    {
        CurrentUserProvider currentUser = new();
        Mock<IRepository> repoMock = new();
        Mock<IFileStorageService> storageMock = new();

        ServiceCollection services = new();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();
        services.AddSingleton(storageMock.Object);
        using ServiceProvider provider = services.BuildServiceProvider();

        DeleteStorageEntryCommand command = new(
            Mock.Of<ILogger<DeleteStorageEntryCommand>>(),
            new DeleteStorageEntryValidator(),
            repoMock.Object,
            provider.GetRequiredService<ServiceLocator>(),
            currentUser);

        using (currentUser.BeginScope(10L))
        {
            Result forbiddenResult = await command.ExecuteAsync(new DeleteStorageEntryRequest { Location = "/storage/u_20/exp/file.csv" });
            Assert.False(forbiddenResult.Success);
            Assert.Equal(StatusCode.Forbidden, forbiddenResult.StatusCode);
            storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task ChannelPipelineProgressNotifier_IsolatesChannelsPerUser_AllowsAnonymousPublicChannel_AndAllowsSuperuserRead()
    {
        CurrentUserProvider currentUser = new();
        PipelineProgressNotifierSettings settings = new()
        {
            ChannelCapacity = 16,
            FullMode = BoundedChannelFullMode.DropOldest,
            MaxChannels = 32
        };

        await using ChannelPipelineProgressNotifier notifier = new(currentUser, TimeProvider.System, settings);

        using (currentUser.BeginScope(10L))
        {
            await notifier.NotifyAsync(new PipelineProgressEvent
            {
                PipelineIdentifier = "shared-id",
                StepName = "Step1",
                Description = "User 10 progress",
                State = PipelineStepState.Running,
                Timestamp = TimeProvider.System.GetUtcNow()
            });
            notifier.TryComplete("shared-id");
        }

        // User 20 cannot read User 10's channel
        using (currentUser.BeginScope(20L))
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await foreach (PipelineProgressEvent _ in notifier.ReadAllAsync("shared-id")) { }
            });
        }

        // Anonymous cannot read User 10's channel
        using (currentUser.BeginScope(null))
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await foreach (PipelineProgressEvent _ in notifier.ReadAllAsync("shared-id")) { }
            });
        }

        // Superuser can inspect User 10's channel
        using (currentUser.BeginScope(1L, isSuperuser: true))
        {
            List<PipelineProgressEvent> events = [];
            await foreach (PipelineProgressEvent item in notifier.ReadAllAsync("shared-id"))
            {
                events.Add(item);
            }

            Assert.Single(events);
            Assert.Equal("User 10 progress", events[0].Description);
        }
    }

    [Fact]
    public async Task RefreshAuthenticationTokenCommand_RejectsInactiveUser()
    {
        JwtSettings jwtSettings = new()
        {
            Issuer = "MelloSilveiraTools.Tests",
            Audience = "MelloSilveiraTools.Tests",
            EncryptionKey = "MelloSilveiraToolsQuickstartKey!",
            SigningKey = "MelloSilveiraToolsSigningKey2026!!",
            ClockSkewInSeconds = 30,
            SecurityKeyType = SecurityKeyType.Symmetric,
            TokenExperitationTimeInMinutes = 60
        };

        AuthenticationJweTokenService tokenService = new(TimeProvider.System, jwtSettings);
        AuthenticationToken activeToken = tokenService.Generate(42L);

        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.GetAsync<UserEntity>(42L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserEntity { Id = 42L, Username = "deactivated", PasswordHash = "hash", IsActive = false });

        RefreshAuthenticationTokenCommand command = new(tokenService, repoMock.Object);
        Result<AuthenticationToken> result = await command.ExecuteAsync(new RefreshTokenRequest { Token = activeToken.Value });

        Assert.False(result.Success);
        Assert.Equal(StatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public void WebApiHost_Settings_Validate_RejectsPlaceholderSecretsOutsideDevelopment()
    {
        string hostDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "MelloSilveiraTools.WebApi.Host"));
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(hostDir, "appsettings.json"), optional: false)
            .AddJsonFile(Path.Combine(hostDir, "secrets.example.json"), optional: false)
            .Build();

        HostSettings? settings = configuration.Get<HostSettings>();
        Assert.NotNull(settings);

        Assert.True(settings.Validate(isDevelopment: true).Success);

        Result productionValidation = settings.Validate(isDevelopment: false);
        Assert.False(productionValidation.Success);
        Assert.Contains(productionValidation.Messages, m => m.Contains("placeholder", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateAndDeleteStorageEntryCommands_BlockAnonymousAndRegularUsersFromModifyingPublicNullUserIdFile()
    {
        CurrentUserProvider currentUser = new();
        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(77L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageEntryMetadataEntity
            {
                Id = 77L,
                UserId = null,
                StorageProvider = StorageProvider.Local,
                Location = "/storage/samples/SamplesData/acl.csv",
                Directory = "/storage/samples/SamplesData",
                FileName = "acl.csv",
                SizeBytes = 100L,
                ExternalIdentifier = "samples",
                ContentType = "text/csv"
            });

        Mock<IFileStorageService> storageMock = new();
        ServiceCollection services = new();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();
        services.AddSingleton(storageMock.Object);
        services.AddKeyedSingleton(StorageProvider.Local, storageMock.Object);
        using ServiceProvider provider = services.BuildServiceProvider();

        Mock<IValidator<UpdateStorageEntryFileRequest>> validatorMock = new();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateStorageEntryFileRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.CreateSuccessOk());

        UpdateStorageEntryFileCommand updateCommand = new(
            Mock.Of<ILogger<UpdateStorageEntryFileCommand>>(),
            validatorMock.Object,
            repoMock.Object,
            provider.GetRequiredService<ServiceLocator>(),
            currentUser);

        DeleteStorageEntryCommand deleteCommand = new(
            Mock.Of<ILogger<DeleteStorageEntryCommand>>(),
            new DeleteStorageEntryValidator(),
            repoMock.Object,
            provider.GetRequiredService<ServiceLocator>(),
            currentUser);

        await using MemoryStream content = new("malicious"u8.ToArray());
        FormFile formFile = new(content, 0, content.Length, "file", "acl.csv");

        // Regular authenticated user cannot overwrite or delete a public (UserId == null) file
        using (currentUser.BeginScope(20L))
        {
            Result<StorageEntryResultData> updateResult = await updateCommand.ExecuteAsync(new UpdateStorageEntryFileRequest { FileId = 77L, File = formFile });
            Assert.False(updateResult.Success);
            Assert.Equal(StatusCode.Forbidden, updateResult.StatusCode);

            Result deleteResult = await deleteCommand.ExecuteAsync(new DeleteStorageEntryRequest { FileId = 77L });
            Assert.False(deleteResult.Success);
            Assert.Equal(StatusCode.Forbidden, deleteResult.StatusCode);
        }

        // Anonymous caller (UserId == null) ALSO cannot overwrite or delete a public (UserId == null) file
        using (currentUser.BeginScope(null))
        {
            Result<StorageEntryResultData> anonUpdate = await updateCommand.ExecuteAsync(new UpdateStorageEntryFileRequest { FileId = 77L, File = formFile });
            Assert.False(anonUpdate.Success);
            Assert.Equal(StatusCode.Forbidden, anonUpdate.StatusCode);

            Result anonDelete = await deleteCommand.ExecuteAsync(new DeleteStorageEntryRequest { FileId = 77L });
            Assert.False(anonDelete.Success);
            Assert.Equal(StatusCode.Forbidden, anonDelete.StatusCode);
        }

        storageMock.Verify(s => s.OpenWriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("/storage/u_20/../u_10/file.csv", StatusCode.BadRequest)]
    [InlineData("/storage/u_10/u_20/file.csv", StatusCode.Forbidden)]
    [InlineData("/storage/u_10", StatusCode.Forbidden)]
    public async Task DeleteStorageEntryCommand_BlocksTraversalAndPartitionSpoofing(string maliciousLocation, StatusCode expectedStatusCode)
    {
        CurrentUserProvider currentUser = new();
        Mock<IRepository> repoMock = new();
        Mock<IFileStorageService> storageMock = new();

        ServiceCollection services = new();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();
        services.AddSingleton(storageMock.Object);
        using ServiceProvider provider = services.BuildServiceProvider();

        DeleteStorageEntryCommand command = new(
            Mock.Of<ILogger<DeleteStorageEntryCommand>>(),
            new DeleteStorageEntryValidator(),
            repoMock.Object,
            provider.GetRequiredService<ServiceLocator>(),
            currentUser);

        using (currentUser.BeginScope(10L))
        {
            Result result = await command.ExecuteAsync(new DeleteStorageEntryRequest { Location = maliciousLocation });
            Assert.False(result.Success);
            Assert.Equal(expectedStatusCode, result.StatusCode);
            storageMock.Verify(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public void CloudStorageServices_ResolveKeysWithForwardSlashes()
    {
        ResiliencePipelineSettings resilienceSettings = new()
        {
            BackoffType = Polly.DelayBackoffType.Constant,
            DelayInMilliseconds = 10,
            MaxRetryAttempts = 1,
            UseJitter = false
        };

        BlobStorageFileStorageService blobService = new(
            Mock.Of<ILogger<BlobStorageFileStorageService>>(),
            Mock.Of<IFileStorageService>(),
            TimeProvider.System,
            Mock.Of<BlobContainerClient>(),
            new BlobStorageSettings { ContainerName = "c", BasePrefix = "tenant-root", ConnectionString = "UseDevelopmentStorage=true" },
            new BlobStorageResiliencePipeline(Mock.Of<ILogger<BlobStorageResiliencePipeline>>(), resilienceSettings));

        AwsS3FileStorageService s3Service = new(
            Mock.Of<ILogger<AwsS3FileStorageService>>(),
            Mock.Of<IFileStorageService>(),
            Mock.Of<IAmazonS3>(),
            TimeProvider.System,
            new AwsS3StorageSettings { BucketName = "b", Region = "us-east-1", BasePrefix = "tenant-root" },
            new AwsS3ResiliencePipeline(Mock.Of<ILogger<AwsS3ResiliencePipeline>>(), resilienceSettings));

        string blobLocation = blobService.ResolveLocation("exp-1", "raw", "strain.csv");
        string s3Location = s3Service.ResolveLocation("exp-1", "raw", "strain.csv");

        Assert.Equal("tenant-root/exp-1/raw/strain.csv", blobLocation);
        Assert.Equal("tenant-root/exp-1/raw/strain.csv", s3Location);
        Assert.DoesNotContain('\\', blobLocation);
        Assert.DoesNotContain('\\', s3Location);
    }

    [Fact]
    public async Task RefreshAuthenticationTokenCommand_RejectsNonNumericSubject_WhenRepositoryIsPresent()
    {
        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        tokenServiceMock.Setup(s => s.GetValidatedSubjectAsync("non-numeric-token")).ReturnsAsync("not-a-user-id");

        Mock<IRepository> repoMock = new();
        RefreshAuthenticationTokenCommand command = new(tokenServiceMock.Object, repoMock.Object);

        Result<AuthenticationToken> result = await command.ExecuteAsync(new RefreshTokenRequest { Token = "non-numeric-token" });
        Assert.False(result.Success);
        Assert.Equal(StatusCode.Unauthorized, result.StatusCode);
        tokenServiceMock.Verify(s => s.RefreshAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StreamEndpoints_WriteNdjsonAsync_Sets404WhenKeyNotFoundBeforeResponseStarts()
    {
        DefaultHttpContext httpContext = new();
        await ThrowingKeyNotFoundAsyncEnumerable<string>()
            .WriteNdjsonAsync(httpContext, Mock.Of<ILogger>(), "PipelineProgress");

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task AddJweAuthentication_AutomaticallyRegistersSuperuserOnlyPolicy()
    {
        JwtSettings jwtSettings = new()
        {
            Issuer = "MelloSilveiraTools.Tests",
            Audience = "MelloSilveiraTools.Tests",
            EncryptionKey = "MelloSilveiraToolsQuickstartKey!",
            SigningKey = "MelloSilveiraToolsSigningKey2026!!",
            ClockSkewInSeconds = 30,
            SecurityKeyType = SecurityKeyType.Symmetric,
            TokenExperitationTimeInMinutes = 60
        };

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICurrentUserProvider, CurrentUserProvider>();
        services.AddSingleton(Mock.Of<IRepository>());
        services.AddJweAuthentication(jwtSettings);

        using ServiceProvider provider = services.BuildServiceProvider();
        IAuthorizationPolicyProvider policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        AuthorizationPolicy? policy = await policyProvider.GetPolicyAsync(AuthorizationPolicies.SuperuserOnly);
        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, r => r is SuperuserRequirement);
    }

    [Fact]
    public async Task SuperuserAuthorizationHandler_AuthorizesOnlyLiveSuperuserScope()
    {
        CurrentUserProvider currentUser = new();
        SuperuserAuthorizationHandler handler = new(currentUser);
        SuperuserRequirement requirement = new();
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "1")], "Bearer"));

        AuthorizationHandlerContext regularContext = new([requirement], principal, resource: null);
        using (currentUser.BeginScope(1L, isSuperuser: false))
        {
            await handler.HandleAsync(regularContext);
        }
        Assert.False(regularContext.HasSucceeded);

        AuthorizationHandlerContext superuserContext = new([requirement], principal, resource: null);
        using (currentUser.BeginScope(1L, isSuperuser: true))
        {
            await handler.HandleAsync(superuserContext);
        }
        Assert.True(superuserContext.HasSucceeded);
    }

    [Fact]
    public async Task DeleteAndUpdateStorageCommands_FailClosedWhenUnauthenticated_AndPreserveOwnerOnSuperuserEdit()
    {
        CurrentUserProvider currentUser = new();
        Mock<IRepository> repoMock = new();
        StorageEntryMetadataEntity tenantFile = new()
        {
            Id = 88L,
            UserId = 20L,
            StorageProvider = StorageProvider.Local,
            Location = "/storage/u_20/exp/data.csv",
            Directory = "/storage/u_20/exp",
            FileName = "data.csv",
            SizeBytes = 10L,
            ExternalIdentifier = "exp",
            ContentType = "text/csv"
        };
        repoMock.Setup(r => r.GetAsync<StorageEntryMetadataEntity>(88L, It.IsAny<CancellationToken>())).ReturnsAsync(tenantFile);

        StorageEntryMetadataEntity? insertedEntity = null;
        repoMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .Callback<StorageEntryMetadataEntity, CancellationToken>((e, _) => insertedEntity = e)
            .ReturnsAsync(Result.CreateSuccessOk(89L));

        Mock<IFileStorageService> storageMock = new();
        storageMock.Setup(s => s.OpenWriteAsync(tenantFile.Location, It.IsAny<CancellationToken>())).ReturnsAsync(() => new MemoryStream());
        storageMock.Setup(s => s.GetMetadataAsync(tenantFile.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, tenantFile.Location, tenantFile.Directory, tenantFile.FileName, 12L, TimeProvider.System.GetUtcNow()));

        ServiceCollection services = new();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();
        services.AddSingleton(storageMock.Object);
        services.AddKeyedSingleton(StorageProvider.Local, storageMock.Object);
        using ServiceProvider provider = services.BuildServiceProvider();

        DeleteStorageEntryCommand deleteCommand = new(
            Mock.Of<ILogger<DeleteStorageEntryCommand>>(),
            new DeleteStorageEntryValidator(),
            repoMock.Object,
            provider.GetRequiredService<ServiceLocator>(),
            currentUser);

        Result unauthDelete = await deleteCommand.ExecuteAsync(new DeleteStorageEntryRequest { Location = "/storage/u_20/exp/data.csv" });
        Assert.False(unauthDelete.Success);
        Assert.Equal(StatusCode.Forbidden, unauthDelete.StatusCode);

        Mock<IValidator<UpdateStorageEntryFileRequest>> updateValidator = new();
        updateValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateStorageEntryFileRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.CreateSuccessOk());

        UpdateStorageEntryFileCommand updateCommand = new(
            Mock.Of<ILogger<UpdateStorageEntryFileCommand>>(),
            updateValidator.Object,
            repoMock.Object,
            provider.GetRequiredService<ServiceLocator>(),
            currentUser);

        await using MemoryStream content = new("updated-data"u8.ToArray());
        FormFile formFile = new(content, 0, content.Length, "file", "data.csv");

        Result<StorageEntryResultData> unauthUpdate = await updateCommand.ExecuteAsync(new UpdateStorageEntryFileRequest { FileId = 88L, File = formFile });
        Assert.False(unauthUpdate.Success);
        Assert.Equal(StatusCode.Forbidden, unauthUpdate.StatusCode);

        // Superuser update preserves original tenant UserId (20L)
        using (currentUser.BeginScope(1L, isSuperuser: true))
        {
            content.Position = 0;
            Result<StorageEntryResultData> superuserUpdate = await updateCommand.ExecuteAsync(new UpdateStorageEntryFileRequest { FileId = 88L, File = formFile });
            Assert.True(superuserUpdate.Success);
            Assert.NotNull(insertedEntity);
            Assert.Equal(20L, insertedEntity.UserId);
        }
    }

    private static async IAsyncEnumerable<T> ThrowingKeyNotFoundAsyncEnumerable<T>()
    {
        await Task.CompletedTask;
        throw new KeyNotFoundException("Pipeline not found.");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
