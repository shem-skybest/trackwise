using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Application.Modules.MasterData.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers.MasterData;

/// <summary>
/// Stewardship of ports. Writes the same records <c>/terminals</c> reads, which keeps that route
/// read-only: a second write path would bypass the duplicate probe, the dependency probe and code
/// immutability, and one bypassable guarantee is no guarantee.
/// <para>
/// Deliberately not gated on <c>TenantFeature.IntermodalContainers</c>, unlike the dispatch-facing
/// terminal routes. Ports are reference data forwarding and documentation need whether or not a
/// tenant moves containers.
/// </para>
/// </summary>
[ApiController]
[Route("master-data/ports")]
[Produces("application/json")]
public class PortController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetPortById")]
    [ProducesResponseType(typeof(TerminalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPortByIdQuery { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetPorts")]
    [ProducesResponseType(typeof(PagedResponse<TerminalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetList([FromQuery] GetPortsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<TerminalDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{id:guid}/history", Name = "GetPortHistory")]
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
            EntityType = nameof(Trackwise.Domain.Entities.Terminal),
            EntityId = id,
            Page = page,
            PageSize = pageSize
        };

        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<MasterDataChangeLogDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreatePort")]
    [ProducesResponseType(typeof(TerminalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Create([FromBody] CreatePortCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);

        if (result.IsSuccess)
        {
            return CreatedAtRoute("GetPortById", new { id = result.Value!.Id }, result.Value);
        }

        return Failure(result);
    }

    private IActionResult Failure(Trackwise.Shared.Models.IResult result) =>
        MasterDataFailure.From(this, result);

    [HttpPut("{id:guid}", Name = "UpdatePort")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePortCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    [HttpDelete("{id:guid}", Name = "DeletePort")]
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
        var command = new DeletePortCommand
        {
            Id = id,
            ConfirmedDespiteDependents = confirmedDespiteDependents,
            DependencyReason = dependencyReason
        };

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    /// <summary>
    /// Folds one port into another. The survivor keeps its code and inherits every reference the
    /// loser held - including the loads and containers that physically pointed at it.
    /// </summary>
    [HttpPost("{id:guid}/merge", Name = "MergePorts")]
    [ProducesResponseType(typeof(TerminalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Merge(
        Guid id, [FromBody] MergePortsCommand request, CancellationToken ct)
    {
        // The route id is the survivor - the record the caller is looking at and keeping.
        request.SurvivorId = id;

        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/restore", Name = "RestorePort")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RestorePortCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }
}
