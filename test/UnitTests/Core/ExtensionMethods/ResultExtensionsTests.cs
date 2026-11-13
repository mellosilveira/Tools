using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
    public async Task AddErrorIf_TaskCondition_ShouldEvaluateCondition()
    {
        Result resultTrue = Result.CreateSuccessOk();
        resultTrue = await resultTrue.AddErrorIf(Task.FromResult(true), "Async condition true.");

        Result resultFalse = Result.CreateSuccessOk();
        resultFalse = await resultFalse.AddErrorIf(Task.FromResult(false), "Async condition false.");

        Assert.False(resultTrue.Success);
        Assert.Contains("Async condition true.", resultTrue.Messages);
        Assert.True(resultFalse.Success);
        Assert.Empty(resultFalse.Messages);
    }

    [Fact]
    public void AddErrorIf_WithFunc_ShouldExecuteFuncWhenTrue()
    {
        Result result = Result.CreateSuccessOk();

        result = result.AddErrorIf(true, r => r.AddError("Custom error func."));

        Assert.False(result.Success);
        Assert.Contains("Custom error func.", result.Messages);
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
    public void AddErrorIfNotNull_WhenNotNull_ShouldAddError()
    {
        Result result = Result.CreateSuccessOk();

        result = result.AddErrorIfNotNull("not-null", "Value must be null.");

        Assert.False(result.Success);
        Assert.Contains("Value must be null.", result.Messages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddErrorIfNullOrEmpty_String_WhenNullOrEmpty_ShouldAddError(string? value)
    {
        Result result = Result.CreateSuccessOk();

        result = result.AddErrorIfNullOrEmpty(value!, "String cannot be empty.");

        Assert.False(result.Success);
        Assert.Contains("String cannot be empty.", result.Messages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddErrorIfNullOrWhiteSpace_WhenNullOrWhitespace_ShouldAddError(string? value)
    {
        Result result = Result.CreateSuccessOk();

        result = result.AddErrorIfNullOrWhiteSpace(value!, "String cannot be whitespace.");

        Assert.False(result.Success);
        Assert.Contains("String cannot be whitespace.", result.Messages);
    }

    [Fact]
    public void AddErrorIfNullOrEmpty_Collection_WhenEmpty_ShouldAddError()
    {
        Result result = Result.CreateSuccessOk();
        List<int> empty = [];

        result = result.AddErrorIfNullOrEmpty(empty, "Collection cannot be empty.");

        Assert.False(result.Success);
        Assert.Contains("Collection cannot be empty.", result.Messages);
    }

    [Fact]
    public void AddErrorIfZero_WhenZero_ShouldAddError()
    {
        Result result = Result.CreateSuccessOk();

        result = result.AddErrorIfZero(0.0, "Value cannot be zero.");

        Assert.False(result.Success);
        Assert.Contains("Value cannot be zero.", result.Messages);
    }

    [Fact]
    public void AddErrorIfNegative_WhenNegative_ShouldAddError()
    {
        Result result = Result.CreateSuccessOk();

        result = result.AddErrorIfNegative(-1.5, "Value cannot be negative.");

        Assert.False(result.Success);
        Assert.Contains("Value cannot be negative.", result.Messages);
    }

    [Fact]
    public void AddErrorIfNegativeOrZero_WhenZeroOrNegative_ShouldAddError()
    {
        Result resultZero = Result.CreateSuccessOk().AddErrorIfNegativeOrZero(0.0, "Cannot be <= 0.");
        Result resultNeg = Result.CreateSuccessOk().AddErrorIfNegativeOrZero(-5.0, "Cannot be <= 0.");
        Result resultPos = Result.CreateSuccessOk().AddErrorIfNegativeOrZero(5.0, "Cannot be <= 0.");

        Assert.False(resultZero.Success);
        Assert.False(resultNeg.Success);
        Assert.True(resultPos.Success);
    }

    [Fact]
    public void AddErrorIfNegativeOrZero_List_ShouldValidateAllElements()
    {
        Result resultEmpty = Result.CreateSuccessOk().AddErrorIfNegativeOrZero(new List<double>(), "Invalid list.");
        Result resultInvalid = Result.CreateSuccessOk().AddErrorIfNegativeOrZero(new List<double> { 1.0, -2.0, 3.0 }, "Contains non-positive.");
        Result resultValid = Result.CreateSuccessOk().AddErrorIfNegativeOrZero(new List<double> { 1.0, 2.0, 3.0 }, "Contains non-positive.");

        Assert.False(resultEmpty.Success);
        Assert.False(resultInvalid.Success);
        Assert.True(resultValid.Success);
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
    public async Task OnSuccess_Async_WhenSuccess_ShouldExecuteAsyncAction()
    {
        Result result = Result.CreateSuccessOk();
        bool executed = false;

        await result.OnSuccess(async () =>
        {
            await Task.Yield();
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
    public async Task OnError_Async_WhenFailed_ShouldExecuteAsyncAction()
    {
        Result result = Result.CreateBadRequest("Initial error.");
        bool executed = false;

        await result.OnError(async () =>
        {
            await Task.Yield();
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

    [Fact]
    public void BuildHttpResponse_ShouldReturnJsonResultWithMatchingStatusCode()
    {
        Result result = Result.CreateSuccessCreated();

        JsonResult jsonResult = result.BuildHttpResponse();

        Assert.Equal(201, jsonResult.StatusCode);
        Assert.Same(result, jsonResult.Value);
    }

    [Fact]
    public async Task BuildHttpResponseAsync_ShouldAwaitAndReturnJsonResult()
    {
        Task<Result> resultTask = Task.FromResult(Result.CreateSuccessOk());

        JsonResult jsonResult = await resultTask.BuildHttpResponseAsync();

        Assert.Equal(200, jsonResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_ShouldReturnIResultWithMatchingStatusCode()
    {
        Result result = Result.CreateNotFound("Item missing");

        IResult httpResult = result.ToHttpResult();

        Assert.NotNull(httpResult);
    }

    [Fact]
    public async Task ToHttpResultAsync_ShouldAwaitAndReturnIResult()
    {
        Task<Result> resultTask = Task.FromResult(Result.CreateBadRequest("Bad request"));

        IResult httpResult = await resultTask.ToHttpResultAsync();

        Assert.NotNull(httpResult);
    }
}
