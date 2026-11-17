using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Add;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Read;
using MelloSilveiraTools.WebApi.Application.Commands.Crud.Update;
using Moq;
using UnitTests.WebApi.Fixtures;

namespace UnitTests.WebApi.Application.Commands.Crud;

public class CrudCommandsTests
{
    [Fact]
    public async Task AddEntity_InsertsViaRepositoryAndReturnsCreatedWithId()
    {
        Mock<IRepository> repositoryMock = new();
        WebApiTestEntity entity = new() { Name = "Created" };
        repositoryMock
            .Setup(r => r.InsertAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(123L);

        AddEntity<WebApiTestEntity> command = new(repositoryMock.Object);
        AddResult result = await command.ExecuteAsync(new AddEntityRequest<WebApiTestEntity> { Entity = entity, ResourceName = "item" });

        Assert.True(result.Success);
        Assert.Equal(StatusCode.Created, result.StatusCode);
        Assert.Equal(123L, result.Data?.Id);
        repositoryMock.Verify(r => r.InsertAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AddResult_CreateConflictAndImplicitConversionFromResult_PreserveProperties()
    {
        AddResult conflict = AddResult.CreateConflict(77L, "Already exists");
        Assert.False(conflict.Success);
        Assert.Equal(StatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(77L, conflict.Data?.Id);
        Assert.Contains("Already exists", conflict.Messages);

        Result generic = Result.CreateBadRequest("Invalid payload");
        AddResult converted = generic;
        Assert.False(converted.Success);
        Assert.Equal(StatusCode.BadRequest, converted.StatusCode);
        Assert.Equal(generic.Messages, converted.Messages);
    }

    [Fact]
    public async Task ReadEntityById_WhenFoundAndWhenNull_ReturnsOkOrNotFound()
    {
        Mock<IRepository> repositoryMock = new();
        WebApiTestEntity entity = new() { Id = 10L, Name = "Existing" };
        repositoryMock.Setup(r => r.GetAsync<WebApiTestEntity>(10L, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repositoryMock.Setup(r => r.GetAsync<WebApiTestEntity>(999L, It.IsAny<CancellationToken>())).ReturnsAsync((WebApiTestEntity?)null);

        ReadEntityById<WebApiTestEntity> command = new(repositoryMock.Object);

        Result<WebApiTestEntity> found = await command.ExecuteAsync(new ReadEntityByIdRequest { Id = 10L, ResourceName = "item" });
        Assert.True(found.Success);
        Assert.Equal(StatusCode.OK, found.StatusCode);
        Assert.Equal(entity, found.Data);

        Result<WebApiTestEntity> missing = await command.ExecuteAsync(new ReadEntityByIdRequest { Id = 999L, ResourceName = "item" });
        Assert.False(missing.Success);
        Assert.Equal(StatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task ReadEntityPaged_ComputesPageNumberForNonEmptyAndEmptyResults()
    {
        Mock<IRepository> repositoryMock = new();
        WebApiTestFilter filter = new() { Name = "a" };
        Pagination paginationWithOffset = new() { Offset = 4, Limit = 2 };

        repositoryMock.Setup(r => r.CountAsync<WebApiTestEntity, WebApiTestFilter>(filter, It.IsAny<CancellationToken>())).ReturnsAsync(10L);
        repositoryMock
            .Setup(r => r.GetAsync<WebApiTestEntity, WebApiTestFilter>(filter, paginationWithOffset, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable([new WebApiTestEntity { Id = 5 }, new WebApiTestEntity { Id = 6 }]));

        ReadEntityPaged<WebApiTestEntity, WebApiTestFilter> command = new(repositoryMock.Object);
        PagedResult<WebApiTestEntity> nonEmpty = await command.ExecuteAsync(new ReadEntityPagedRequest<WebApiTestFilter>
        {
            Filter = filter,
            Pagination = paginationWithOffset,
            ResourceName = "item"
        });

        Assert.True(nonEmpty.Success);
        Assert.Equal(StatusCode.OK, nonEmpty.StatusCode);
        Assert.Equal(10L, nonEmpty.TotalCount);
        Assert.Equal(3, nonEmpty.PageNumber);
        Assert.Equal(2, nonEmpty.Data?.Count);

        Pagination defaultPagination = new();
        repositoryMock.Setup(r => r.CountAsync<WebApiTestEntity, WebApiTestFilter>(filter, It.IsAny<CancellationToken>())).ReturnsAsync(0L);
        repositoryMock
            .Setup(r => r.GetAsync<WebApiTestEntity, WebApiTestFilter>(filter, defaultPagination, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable([]));

        PagedResult<WebApiTestEntity> empty = await command.ExecuteAsync(new ReadEntityPagedRequest<WebApiTestFilter>
        {
            Filter = filter,
            Pagination = defaultPagination,
            ResourceName = "item"
        });

        Assert.True(empty.Success);
        Assert.Equal(1, empty.PageNumber);
        Assert.Empty(empty.Data!);
    }

    [Fact]
    public async Task UpdateEntity_SetsIdAndReturnsCreatedOrNoContent()
    {
        Mock<IRepository> repositoryMock = new();
        repositoryMock
            .Setup(r => r.TryUpdateAsync(It.Is<WebApiTestEntity>(e => e.Id == 55L && e.Name == "Updated"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repositoryMock
            .Setup(r => r.TryUpdateAsync(It.Is<WebApiTestEntity>(e => e.Id == 56L), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UpdateEntity<WebApiTestEntity> command = new(repositoryMock.Object);

        Result updated = await command.ExecuteAsync(new UpdateEntityRequest<WebApiTestEntity>
        {
            Id = 55L,
            Entity = new WebApiTestEntity { Id = 1L, Name = "Updated" },
            ResourceName = "item"
        });
        Assert.Equal(StatusCode.Created, updated.StatusCode);

        Result missing = await command.ExecuteAsync(new UpdateEntityRequest<WebApiTestEntity>
        {
            Id = 56L,
            Entity = new WebApiTestEntity { Name = "Missing" },
            ResourceName = "item"
        });
        Assert.Equal(StatusCode.NoContent, missing.StatusCode);
    }

    [Fact]
    public async Task DeleteEntity_CallsRepositoryAndReturnsOk()
    {
        Mock<IRepository> repositoryMock = new();
        repositoryMock.Setup(r => r.DeleteAsync<WebApiTestEntity>(88L, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        DeleteEntity<WebApiTestEntity> command = new(repositoryMock.Object);
        Result result = await command.ExecuteAsync(new DeleteEntityRequest { Id = 88L, ResourceName = "item" });

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        repositoryMock.Verify(r => r.DeleteAsync<WebApiTestEntity>(88L, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<WebApiTestEntity> ToAsyncEnumerable(WebApiTestEntity[] items)
    {
        foreach (WebApiTestEntity item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
