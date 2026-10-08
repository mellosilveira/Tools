using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;

namespace UnitTests.Core.ExtensionMethods;

public class ResultExtensionsTests
{
    [Fact]
    public void AddError_ShouldAppendMessageAndSetFailure()
    {
        Result result = Result.CreateSuccessOk();

        result = result.AddError("Something failed.", StatusCode.BadRequest);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        Assert.Contains("Something failed.", result.Messages);
    }

    [Fact]
    public void AddErrorIf_WhenConditionIsTrue_ShouldAddError()
    {
        Result result = Result.CreateSuccessOk();

        result = result.AddErrorIf(true, "Condition was true.");

        Assert.False(result.Success);
        Assert.Contains("Condition was true.", result.Messages);
    }

    [Fact]
    public void AddErrorIf_WhenConditionIsFalse_ShouldRemainSuccessful()
    {
        Result result = Result.CreateSuccessOk();

        result = result.AddErrorIf(false, "Should not be added.");

        Assert.True(result.Success);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void AddErrorIfNull_WhenNull_ShouldAddError()
    {
        Result result = Result.CreateSuccessOk();

        result = result.AddErrorIfNull(null!, "Value cannot be null.");

        Assert.False(result.Success);
        Assert.Contains("Value cannot be null.", result.Messages);
    }

    [Fact]
    public void OnSuccess_WhenSuccess_ShouldExecuteAction()
    {
        Result result = Result.CreateSuccessOk();
        bool executed = false;

        result.OnSuccess(() =>
        {
            executed = true;
            return Result.CreateSuccessOk();
        });

        Assert.True(executed);
    }

    [Fact]
    public void OnError_WhenFailed_ShouldExecuteAction()
    {
        Result result = Result.CreateBadRequest("Initial error.");
        bool executed = false;

        result.OnError(() =>
        {
            executed = true;
            return Result.CreateBadRequest("Handled error.");
        });

        Assert.True(executed);
    }

    [Fact]
    public async Task Match_ShouldBranchBasedOnSuccess()
    {
        Result success = Result.CreateSuccessOk();
        Result failure = Result.CreateBadRequest("Failed");

        Result successHandled = await success.Match(
            onSuccess: () => Task.FromResult(Result.CreateSuccessCreated()),
            onError: () => Task.FromResult(Result.CreateBadRequest("Failed branch"))
        );

        Result failureHandled = await failure.Match(
            onSuccess: () => Task.FromResult(Result.CreateSuccessCreated()),
            onError: () => Task.FromResult(Result.CreateNotFound("Handled not found"))
        );

        Assert.Equal(StatusCode.Created, successHandled.StatusCode);
        Assert.Equal(StatusCode.NotFound, failureHandled.StatusCode);
    }
}
