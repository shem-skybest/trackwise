using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Application.Modules.MasterData.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers.MasterData;

/// <summary>
/// Stewardship of helpers. No read-only counterpart elsewhere in the API - a helper has never had a
/// record before, the same starting point Vessel and Trailer had.
/// </summary>
[ApiController]
[Route("master-data/helpers")]
[Produces("application/json")]
public class HelperController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetHelperById")]
    [ProducesResponseType(typeof(HelperDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetHelperByIdQuery { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetHelpers")]
    [ProducesResponseType(typeof(PagedResponse<HelperDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetList([FromQuery] GetHelpersQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<HelperDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{id:guid}/history", Name = "GetHelperHistory")]
    [ProducesResponseType(typeof(PagedResponse<MasterDataChangeLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        // Paging is bound as primitives rather than as the query object: binding the object would
        // publish EntityType and EntityId as query parameters, and both come from the route.
        var query = new GetMasterDataChangeLogQuery
        {
            EntityType = nameof(Trackwise.Domain.Entities.Helper),
            EntityId = id,
            Page = page,
            PageSize = pageSize
        };

        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<MasterDataChangeLogDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateHelper")]
    [ProducesResponseType(typeof(HelperDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHelperCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);

        if (result.IsSuccess)
        {
            return CreatedAtRoute("GetHelperById", new { id = result.Value!.Id }, result.Value);
        }

        return MasterDataFailure.From(this, result);
    }

    [HttpPut("{id:guid}", Name = "UpdateHelper")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateHelperCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : MasterDataFailure.From(this, result);
    }

    [HttpDelete("{id:guid}", Name = "DeleteHelper")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] bool confirmedDespiteDependents = false,
        [FromQuery] string? dependencyReason = null,
        CancellationToken ct = default)
    {
        // Bound as primitives rather than a body: DELETE carries no body through most clients, and
        // the override is a decision the caller is making about this route's id.
        var command = new DeleteHelperCommand
        {
            Id = id,
            ConfirmedDespiteDependents = confirmedDespiteDependents,
            DependencyReason = dependencyReason
        };

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? NoContent() : MasterDataFailure.From(this, result);
    }

    /// <summary>
    /// Folds one helper into another. The survivor keeps its code and inherits every reference the
    /// loser held.
    /// </summary>
    [HttpPost("{id:guid}/merge", Name = "MergeHelpers")]
    [ProducesResponseType(typeof(HelperDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Merge(
        Guid id, [FromBody] MergeHelpersCommand request, CancellationToken ct)
    {
        // The route id is the survivor - the record the caller is looking at and keeping.
        request.SurvivorId = id;

        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : MasterDataFailure.From(this, result);
    }

    [HttpPost("{id:guid}/restore", Name = "RestoreHelper")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RestoreHelperCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : MasterDataFailure.From(this, result);
    }
}
