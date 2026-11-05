using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Application.Modules.MasterData.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers.MasterData;

/// <summary>
/// Stewardship of vessels. The first list here with no read-only counterpart elsewhere in the API,
/// because a vessel has never had a record before - forwarding reads it through these routes or
/// not at all.
/// </summary>
[ApiController]
[Route("master-data/vessels")]
[Produces("application/json")]
public class VesselController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetVesselById")]
    [ProducesResponseType(typeof(VesselDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetVesselByIdQuery { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetVessels")]
    [ProducesResponseType(typeof(PagedResponse<VesselDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetList([FromQuery] GetVesselsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<VesselDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{id:guid}/history", Name = "GetVesselHistory")]
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
            EntityType = nameof(Trackwise.Domain.Entities.Vessel),
            EntityId = id,
            Page = page,
            PageSize = pageSize
        };

        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<MasterDataChangeLogDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateVessel")]
    [ProducesResponseType(typeof(VesselDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVesselCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);

        if (result.IsSuccess)
        {
            return CreatedAtRoute("GetVesselById", new { id = result.Value!.Id }, result.Value);
        }

        return MasterDataFailure.From(this, result);
    }

    [HttpPut("{id:guid}", Name = "UpdateVessel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateVesselCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : MasterDataFailure.From(this, result);
    }

    [HttpDelete("{id:guid}", Name = "DeleteVessel")]
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
        var command = new DeleteVesselCommand
        {
            Id = id,
            ConfirmedDespiteDependents = confirmedDespiteDependents,
            DependencyReason = dependencyReason
        };

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? NoContent() : MasterDataFailure.From(this, result);
    }

    /// <summary>
    /// Folds one vessel into another. The survivor keeps its code and inherits every reference the
    /// loser held.
    /// </summary>
    [HttpPost("{id:guid}/merge", Name = "MergeVessels")]
    [ProducesResponseType(typeof(VesselDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Merge(
        Guid id, [FromBody] MergeVesselsCommand request, CancellationToken ct)
    {
        // The route id is the survivor - the record the caller is looking at and keeping.
        request.SurvivorId = id;

        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/restore", Name = "RestoreVessel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RestoreVesselCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }
}
