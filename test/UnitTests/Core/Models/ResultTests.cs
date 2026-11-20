using MelloSilveiraTools.Core.Models;

namespace UnitTests.Core.Models;

public class ResultTests
{
    [Fact]
    public void CreateSuccessOk_ShouldReturnSuccessWithOkStatus()
    {
        Result result = Result.CreateSuccessOk();

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void CreateSuccessCreated_ShouldReturnSuccessWithCreatedStatus()
    {
        Result result = Result.CreateSuccessCreated();

        Assert.True(result.Success);
        Assert.Equal(StatusCode.Created, result.StatusCode);
    }

    [Fact]
    public void CreateNoContent_ShouldReturnSuccessWithNoContentStatus()
    {
        Result result = Result.CreateNoContent();

        Assert.True(result.Success);
        Assert.Equal(StatusCode.NoContent, result.StatusCode);
    }

    [Fact]
    public void CreateBadRequest_WithMessage_ShouldReturnFailureWithBadRequestStatus()
    {
        string errorMessage = "Invalid business input provided.";
        Result result = Result.CreateBadRequest(errorMessage);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        Assert.Single(result.Messages);
        Assert.Equal(errorMessage, result.Messages[0]);
    }

    [Fact]
    public void CreateBadRequest_WithMultipleMessages_ShouldReturnFailureWithBadRequestStatus()
    {
        List<string> errorMessages = ["Error 1", "Error 2"];
        Result result = Result.CreateBadRequest<Result>(errorMessages);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        Assert.Equal(2, result.Messages.Count);
    }

    [Fact]
    public void CreateUnauthorized_ShouldReturnFailureWithUnauthorizedStatus()
    {
        Result result = Result.CreateUnauthorized("Authentication token is required.");

        Assert.False(result.Success);
        Assert.Equal(StatusCode.Unauthorized, result.StatusCode);
        Assert.Contains("Authentication token is required.", result.Messages);
    }

    [Fact]
    public void CreateNotFound_ShouldReturnFailureWithNotFoundStatus()
    {
        Result result = Result.CreateNotFound("Requested business entity does not exist.");

        Assert.False(result.Success);
        Assert.Equal(StatusCode.NotFound, result.StatusCode);
        Assert.Contains("Requested business entity does not exist.", result.Messages);
    }

    [Fact]
    public void CreateConflict_ShouldSetConflictFlagAndStatusCode()
    {
        string entity = "ExistingEntity";
        Result<string> result = Result.CreateConflict(entity, "Duplicate registration.");

        Assert.False(result.Success);
        Assert.True(result.IsConflict);
        Assert.Equal(StatusCode.Conflict, result.StatusCode);
        Assert.Equal(entity, result.Data);
        Assert.Contains("Duplicate registration.", result.Messages);
    }

    [Fact]
    public void CreateSuccessOk_WithData_ShouldStorePayload()
    {
        int payload = 42;
        Result<int> result = Result.CreateSuccessOk(payload);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.Equal(payload, result.Data);
    }

    [Fact]
    public void ChangeData_ShouldUpdateDataWhileRetainingStatusAndMessages()
    {
        Result<int> original = Result.CreateSuccessOk(100);
        Result<string> modified = original.ChangeData("One Hundred");

        Assert.True(modified.Success);
        Assert.Equal(StatusCode.OK, modified.StatusCode);
        Assert.Equal("One Hundred", modified.Data);
    }

    [Fact]
    public void CreateListedSuccessOk_ShouldPopulateDataAndCount()
    {
        List<string> items = ["ItemA", "ItemB", "ItemC"];
        ListedResult<string> result = Result.CreateListedSuccessOk(items);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.Equal(3, result.Count);
        Assert.Equal(items, result.Data);
    }

    [Fact]
    public void CreatePagedSuccessOk_ShouldPopulatePagedResult()
    {
        List<int> pageItems = [1, 2, 3];
        PagedResult<int> result = Result.CreatePagedSuccessOk(pageItems);

        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.Equal(pageItems, result.Data);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ImplicitOperator_FromData_ShouldProduceSuccessResult()
    {
        string message = "DirectValue";
        Result<string> result = message;

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.Equal(message, result.Data);
    }

    [Fact]
    public void AdditionalFactoryMethodsAndConversions_CoverAllResultVariants()
    {
        Result unauthorizedNoMsg = Result.CreateUnauthorized();
        Result notFoundNoMsg = Result.CreateNotFound();
        Result timeout = Result.CreateRequestTimeout("Timed out");
        Result unprocessable = Result.CreateUnprocessableEntity("Invalid state");
        Result unknownNoMsg = Result.CreateUnknownError();
        Result unknownWithMsg = Result.CreateUnknownError("Unexpected failure");
        Result unavailable = Result.CreateServiceUnavailable("Maintenance");
        Result<string> genericUnknown = Result.CreateUnknownError<Result<string>>("Typed unknown");

        Assert.Equal(StatusCode.Unauthorized, unauthorizedNoMsg.StatusCode);
        Assert.Empty(unauthorizedNoMsg.Messages);
        Assert.Equal(StatusCode.NotFound, notFoundNoMsg.StatusCode);
        Assert.Equal(StatusCode.RequestTimeout, timeout.StatusCode);
        Assert.Equal(StatusCode.UnprocessableEntity, unprocessable.StatusCode);
        Assert.Equal(StatusCode.UnknownError, unknownNoMsg.StatusCode);
        Assert.Contains("Unexpected failure", unknownWithMsg.Messages);
        Assert.Equal(StatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.Equal(StatusCode.UnknownError, genericUnknown.StatusCode);

        Result multiMessageBadRequest = Result.CreateBadRequest(["M1", "M2"]);
        Assert.Equal($"M1{Environment.NewLine}M2", multiMessageBadRequest.JoinedMessages);

        Result<int> accepted = Result.CreateSuccessAccepted(7);
        Result<int> created = Result.CreateSuccessCreated(8);
        Result<string> conflictNoMsg = Result.CreateConflict("dup");
        Assert.Equal(StatusCode.Accepted, accepted.StatusCode);
        Assert.Equal(StatusCode.Created, created.StatusCode);
        Assert.True(conflictNoMsg.IsConflict);
        Assert.Empty(conflictNoMsg.Messages);

        Result convertedFromGeneric = Result.Create(accepted);
        Result<double> convertedToGeneric = Result.Create<Result<double>>(multiMessageBadRequest);
        Assert.True(convertedFromGeneric.Success);
        Assert.Equal(2, convertedToGeneric.Messages.Count);

        Result<string> emptyDataResult = Result.CreateError(StatusCode.BadRequest, "err");
        Result<int> changedWhenNullData = emptyDataResult.ChangeData(42);
        Assert.False(changedWhenNullData.Success);
        Assert.Equal(default, changedWhenNullData.Data);

        ListedResult<int> listedNull = Result.CreateListedSuccessOk<int>();
        Assert.Equal(0, listedNull.Count);

        ListedResult<int> listedImplicit = multiMessageBadRequest;
        PagedResult<int> pagedImplicit = multiMessageBadRequest;
        Assert.Equal(StatusCode.BadRequest, listedImplicit.StatusCode);
        Assert.Equal(StatusCode.BadRequest, pagedImplicit.StatusCode);

        PagedResult<int> pagedWithMeta = Result.CreatePagedSuccessOk([1, 2]) with
        {
            TotalCount = 10,
            PageNumber = 2,
            PageSize = 5
        };
        PagedResult<int> pagedNull = Result.CreatePagedSuccessOk<int>();
        Assert.Equal(10, pagedWithMeta.TotalCount);
        Assert.Equal(2, pagedWithMeta.PageNumber);
        Assert.Equal(5, pagedWithMeta.PageSize);
        Assert.Null(pagedNull.Data);

        Result resultNullMsgs = new() { Success = true, StatusCode = StatusCode.OK, Messages = null! };
        Result<int> convertedNullMsgs = Result.Create<Result<int>>(resultNullMsgs);
        Result backFromGenericNullMsgs = Result.Create(new Result<int> { Success = true, StatusCode = StatusCode.OK, Messages = null! });
        Assert.Empty(convertedNullMsgs.Messages);
        Assert.Empty(backFromGenericNullMsgs.Messages);
    }
}
