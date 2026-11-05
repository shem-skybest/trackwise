using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.Operations.Terminals.Queries;

namespace Trackwise.API.Controllers;

/// <summary>
/// The dispatch-facing read of a terminal, for pickers and lookups that should only ever offer live
/// records.
/// <para>
/// Read-only on purpose. A terminal is the same record as a master-data port, and maintaining it
/// belongs to <c>/master-data/ports</c>, where the write goes through the duplicate and dependency
/// probes. Writing here as well would let a steward create a duplicate, rename a code other systems
/// join on, or delete past a dependency - the exact guarantees the module exists to provide.
/// </para>
/// </summary>
[ApiController]
[Route("terminals")]
[Produces("application/json")]
public class TerminalController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetTerminalById")]
    [ProducesResponseType(typeof(TerminalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Terminal.View)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await mediator.Send(new GetTerminalByIdQuery { Id = id });
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetTerminals")]
    [ProducesResponseType(typeof(PagedResponse<TerminalDto>), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.Terminal.View)]
    public async Task<IActionResult> GetList([FromQuery] GetTerminalsQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(PagedResponse<TerminalDto>.FromPagedResult(result, query.Page, query.PageSize));
    }
}
