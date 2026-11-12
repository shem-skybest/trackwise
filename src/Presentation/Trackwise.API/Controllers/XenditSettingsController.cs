using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.Financial.PaymentGateways;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;

namespace Trackwise.API.Controllers;

/// <summary>
/// The company's own Xendit account for online invoice payments (Philippines).
/// </summary>
[ApiController]
[Route("payment-gateways/xendit")]
[Produces("application/json")]
public class XenditSettingsController(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = "GetXenditSettings")]
    [ProducesResponseType(typeof(XenditSettingsDto), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.Payment.View)]
    public async Task<IActionResult> Get()
    {
        var result = await mediator.Send(new GetXenditSettingsQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut(Name = "SaveXenditSettings")]
    [ProducesResponseType(typeof(XenditSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Payment.Manage)]
    public async Task<IActionResult> Save([FromBody] SaveXenditSettingsCommand request)
    {
        var result = await mediator.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete(Name = "DeleteXenditSettings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Permission.Payment.Manage)]
    public async Task<IActionResult> Delete()
    {
        var result = await mediator.Send(new DeleteXenditSettingsCommand());
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }
}
