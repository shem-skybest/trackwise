using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

namespace Trackwise.API.Controllers;

/// <summary>
/// Read-only feed of trading counterparties, for the workfile's shipping-line / trucker / broker
/// pickers.
/// <para>
/// ⚠️ <b>Every write moved to <c>/master-data/partners</c></b> when partners became master data -
/// the same thing that happened to <c>/terminals</c> when ports did. The old create, update and
/// delete here bypassed the duplicate probe, the dependency probe, the change log and, now,
/// approval; leaving them would have let a clerk create a duplicate partner, rename a legacy join
/// key and publish a record nobody reviewed, all without the pipeline noticing.
/// </para>
/// <para>
/// This feed returns <b>approved partners only</b>. A draft is not a company anybody has agreed
/// exists yet, so it has no business appearing in a picker on a live file.
/// </para>
/// </summary>
[ApiController]
[Route("business-partners")]
[Produces("application/json")]
public class BusinessPartnerController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetBusinessPartnerById")]
    [ProducesResponseType(typeof(BusinessPartnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.BusinessPartner.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBusinessPartnerByIdQuery { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetBusinessPartners")]
    [ProducesResponseType(typeof(PagedResponse<BusinessPartnerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.BusinessPartner.View)]
    public async Task<IActionResult> GetList(
        [FromQuery] GetBusinessPartnersQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<BusinessPartnerDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }
}
