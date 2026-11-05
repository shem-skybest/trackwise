using Trackwise.Application.Modules.Compliance.Inspections.Commands;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("vins")]
[Produces("application/json")]
public class VinsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{vin}", Name = "DecodeVin")]
    [ProducesResponseType(typeof(VehicleInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize]
    public async Task<IActionResult> DecodeVin(string vin)
    {
        var result = await mediator.Send(new DecodeVinCommand { Vin = vin });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }
}
