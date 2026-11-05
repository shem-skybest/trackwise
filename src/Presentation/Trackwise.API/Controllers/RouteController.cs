using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.Operations.Routing.Queries;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("routes")]
[Produces("application/json")]
public class RouteController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Ferry-aware route between two points - a plain single driving leg for an ordinary route, or
    /// a multi-leg drive+ferry breakdown when origin/destination sit on different (PH) islands.
    /// </summary>
    [HttpGet("segments", Name = "GetRouteSegments")]
    [ProducesResponseType(typeof(FerryAwareRouteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.View)]
    public async Task<IActionResult> GetRouteSegments([FromQuery] GetRouteSegmentsQuery request)
    {
        var result = await mediator.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }
}
