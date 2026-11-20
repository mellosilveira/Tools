using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using Moq;

namespace UnitTests.Core.Application.Commands;

public class CommandBaseTests
{
    public record SampleRequest(string Name);
    public record SampleItem(int Id, string Title);

    private class SampleCommand(IValidator<SampleRequest>? validator = null) : CommandBaseWithData<SampleRequest, string>(validator)
    {
        public bool Executed { get; private set; }

        protected override Task<Result<string>> ExecuteCommandAsync(SampleRequest request, CancellationToken cancellationToken = default)
        {
            Executed = true;
            return Task.FromResult(Result.CreateSuccessOk($"Hello, {request.Name}"));
        }
    }

    private class SampleDefaultCommand : DefaultCommandBase
    {
        protected override Task<Result> ExecuteCommandAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.CreateSuccessOk());
        }
    }

    private class SampleListedCommand(IValidator<SampleRequest>? validator = null) : ListedCommandBase<SampleRequest, SampleItem>(validator)
    {
        protected override Task<ListedResult<SampleItem>> ExecuteCommandAsync(SampleRequest request, CancellationToken cancellationToken = default)
        {
            List<SampleItem> items = [new(1, request.Name), new(2, $"{request.Name}-2")];
            return Task.FromResult(Result.CreateListedSuccessOk(items));
        }
    }

    private class SamplePagedCommand(IValidator<SampleRequest>? validator = null) : PagedCommandBase<SampleRequest, SampleItem>(validator)
    {
        protected override Task<PagedResult<SampleItem>> ExecuteCommandAsync(SampleRequest request, CancellationToken cancellationToken = default)
        {
            List<SampleItem> items = [new(1, request.Name)];
            PagedResult<SampleItem> paged = Result.CreatePagedSuccessOk(items) with
            {
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 1
            };
            return Task.FromResult(paged);
        }
    }

    private class SampleCommandWithDefaultResponse(IValidator<SampleRequest>? validator = null) : CommandBaseWithDefaultResponse<SampleRequest>(validator)
    {
        protected override Task<Result> ExecuteCommandAsync(SampleRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.CreateSuccess(StatusCode.NoContent));
        }
    }

    private class SampleCommandWithoutRequest : CommandBaseWithoutRequest<string>
    {
        protected override Task<Result<string>> ExecuteCommandAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.CreateSuccessOk("NoRequestResult"));
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_ShouldShortCircuitAndReturnValidationError()
    {
        Mock<IValidator<SampleRequest>> validatorMock = new();
        validatorMock.Setup(v => v.Validate(It.IsAny<SampleRequest>()))
            .Returns(Result.CreateBadRequest("Name is mandatory."));

        SampleCommand command = new(validatorMock.Object);
        SampleRequest request = new(string.Empty);

        Result<string> response = await command.ExecuteAsync(request);

        Assert.False(response.Success);
        Assert.Equal(StatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Name is mandatory.", response.Messages);
        Assert.False(command.Executed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationPasses_ShouldExecuteDomainLogic()
    {
        Mock<IValidator<SampleRequest>> validatorMock = new();
        validatorMock.Setup(v => v.Validate(It.IsAny<SampleRequest>()))
            .Returns(Result.CreateSuccessOk());

        SampleCommand command = new(validatorMock.Object);
        SampleRequest request = new("Bruno");

        Result<string> response = await command.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Equal(StatusCode.OK, response.StatusCode);
        Assert.Equal("Hello, Bruno", response.Data);
        Assert.True(command.Executed);
    }

    [Fact]
    public async Task ExecuteAsync_DefaultCommandBase_ShouldExecuteSuccessfully()
    {
        SampleDefaultCommand command = new();

        Result response = await command.ExecuteAsync();

        Assert.True(response.Success);
        Assert.Equal(StatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExecuteAsync_ListedCommandBase_ShouldReturnListedResult()
    {
        SampleListedCommand command = new();
        SampleRequest request = new("Item");

        ListedResult<SampleItem> response = await command.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Equal(StatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);
        Assert.Equal("Item", response.Data[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_PagedCommandBase_ShouldReturnPagedResult()
    {
        SamplePagedCommand command = new();
        SampleRequest request = new("PagedItem");

        PagedResult<SampleItem> response = await command.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Equal(StatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal(1, response.PageNumber);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task ExecuteAsync_CommandWithDefaultResponse_ShouldExecuteSuccessfully()
    {
        SampleCommandWithDefaultResponse command = new();
        SampleRequest request = new("NoContentItem");

        Result response = await command.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Equal(StatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ExecuteAsync_CommandWithoutRequest_ShouldExecuteSuccessfully()
    {
        SampleCommandWithoutRequest command = new();

        Result<string> response = await command.ExecuteAsync();

        Assert.True(response.Success);
        Assert.Equal(StatusCode.OK, response.StatusCode);
        Assert.Equal("NoRequestResult", response.Data);
    }
}
