using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("charge-codes")]
[Produces("application/json")]
public class ChargeCodeController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetChargeCodeById")]
    [ProducesResponseType(typeof(ChargeCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.ChargeCode.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetChargeCodeByIdQuery { Id = id }, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetChargeCodes")]
    [ProducesResponseType(typeof(IEnumerable<ChargeCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.ChargeCode.View)]
    public async Task<IActionResult> GetList([FromQuery] GetChargeCodesQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateChargeCode")]
    [ProducesResponseType(typeof(ChargeCodeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.ChargeCode.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateChargeCodeCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetChargeCodeById", new { id = result.Value.Id }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }
}
