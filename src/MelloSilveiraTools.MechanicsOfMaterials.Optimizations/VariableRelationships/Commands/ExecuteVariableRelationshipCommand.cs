using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Commands;

/// <summary>
/// Application command that orchestrates the execution of the Variable Relationship and Iso-Stress exploration pipeline via <see cref="IPipeline{TInput, TOutput}"/>.
/// </summary>
/// <param name="pipeline">The canonical variable relationship pipeline.</param>
/// <param name="validator">Optional request validator for <see cref="ExecuteVariableRelationshipRequest"/>.</param>
public sealed class ExecuteVariableRelationshipCommand(
    IPipeline<VariableRelationshipInput, VariableRelationshipOutput> pipeline,
    IValidator<ExecuteVariableRelationshipRequest>? validator = null)
    : CommandBaseWithData<ExecuteVariableRelationshipRequest, ExecuteVariableRelationshipResponse>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<ExecuteVariableRelationshipResponse>> ExecuteCommandAsync(
        ExecuteVariableRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        Result<VariableRelationshipOutput> pipelineResult = await pipeline.ProcessAsync(request.ToInput(), cancellationToken).ConfigureAwait(false);

        return pipelineResult.Success
            ? new ExecuteVariableRelationshipResponse(pipelineResult.Data!)
            : new Result<ExecuteVariableRelationshipResponse> { StatusCode = pipelineResult.StatusCode, Messages = pipelineResult.Messages };
    }
}
