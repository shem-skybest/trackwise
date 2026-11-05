using Trackwise.Application.Modules.Operations.RateAgreements.Commands;
using Trackwise.Application.Modules.Operations.RateAgreements.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers;

/// <summary>The diesel price fuel surcharges are read against.</summary>
[ApiController]
[Route("diesel-prices")]
[Produces("application/json")]
public class DieselPriceController(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = "GetDieselPrices")]
    [ProducesResponseType(typeof(PagedResponse<DieselPriceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tariff.View)]
    public async Task<IActionResult> GetList([FromQuery] GetDieselPricesQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<DieselPriceDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "SetDieselPrice")]
    [ProducesResponseType(typeof(DieselPriceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> Set([FromBody] SetDieselPriceCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }
}
