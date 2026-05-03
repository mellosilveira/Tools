using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.WebApi.Application.Operations;
using Moq;

namespace UnitTests;

public class Class1
{
    [Fact]
    public async Task A()
    {
        // Arrange
        var operation = new FailedValidatioinOperation(Mock.Of<ILogger>());

        // Act
        var response = await operation.ProcessAsync(new OperationRequestBase());
    }

    public class SuccessOperation(ILogger logger) : OperationBaseWithData<OperationRequestBase, MechanicalModelInput>(logger)
    {
        protected override Task<OperationResponse<MechanicalModelInput>> ProcessOperationAsync(OperationRequestBase request)
            => OperationResponse.CreateSuccessOk(new MechanicalModelInput()).AsTask();
        
        protected override Task<OperationResponse<MechanicalModelInput>> ValidateOperationAsync(OperationRequestBase request) 
            => OperationResponse.CreateSuccessOk<MechanicalModelInput>().AsTask();
    }

    public class FailedValidatioinOperation(ILogger logger) : OperationBaseWithData<OperationRequestBase, MechanicalModelInput>(logger)
    {
        protected override Task<OperationResponse<MechanicalModelInput>> ProcessOperationAsync(OperationRequestBase request)
            => OperationResponse.CreateSuccessOk(new MechanicalModelInput()).AsTask();

        protected override Task<OperationResponse<MechanicalModelInput>> ValidateOperationAsync(OperationRequestBase request) 
            => Task.FromResult<OperationResponse<MechanicalModelInput>>(OperationResponse.CreateInternalServerError("Deu ruim."));
    }

    public class FailedProcessOperation(ILogger logger) : OperationBaseWithData<OperationRequestBase, MechanicalModelInput>(logger)
    {
        protected override Task<OperationResponse<MechanicalModelInput>> ProcessOperationAsync(OperationRequestBase request)
            => throw new Exception("Deu ruim");

        protected override Task<OperationResponse<MechanicalModelInput>> ValidateOperationAsync(OperationRequestBase request)
            => OperationResponse.CreateSuccessOk<MechanicalModelInput>().AsTask();
    }
}