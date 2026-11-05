using Trackwise.Application.Modules.Platform.AISettings.Commands;
using Trackwise.Application.Modules.Platform.AISettings.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("ai/settings")]
[Produces("application/json")]
public class AISettingsController(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = "GetAISettings")]
    [ProducesResponseType(typeof(AISettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tenant.Manage)]
    public async Task<IActionResult> GetAISettings()
    {
        var result = await mediator.Send(new GetAISettingsQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut(Name = "UpdateAISettings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tenant.Manage)]
    public async Task<IActionResult> UpdateAISettings([FromBody] UpdateAISettingsCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }
}
