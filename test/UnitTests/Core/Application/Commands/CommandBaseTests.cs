using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using Moq;

namespace UnitTests.Core.Application.Commands;

public class CommandBaseTests
{
    public record SampleRequest(string Name);

    private class SampleCommand(IValidator<SampleRequest>? validator = null) : CommandBaseWithData<SampleRequest, string>(validator)
    {
        public bool Executed { get; private set; }

        protected override Task<Result<string>> ExecuteCommandAsync(SampleRequest request)
        {
            Executed = true;
            return Task.FromResult(Result.CreateSuccessOk($"Hello, {request.Name}"));
        }
    }

    private class SampleDefaultCommand : DefaultCommandBase
    {
        protected override Task<Result> ExecuteCommandAsync()
        {
            return Task.FromResult(Result.CreateSuccessOk());
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
}
