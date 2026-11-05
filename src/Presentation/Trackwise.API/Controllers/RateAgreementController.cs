using Trackwise.Application.Modules.Operations.RateAgreements.Commands;
using Trackwise.Application.Modules.Operations.RateAgreements.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers;

/// <summary>
/// Clients' truck-trip rate agreements. Guarded by the tariff permissions: both are the company's
/// selling rates, and splitting them would hand one person half the price list.
/// </summary>
[ApiController]
[Route("rate-agreements")]
[Produces("application/json")]
public class RateAgreementController(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = "GetRateAgreements")]
    [ProducesResponseType(typeof(PagedResponse<RateAgreementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tariff.View)]
    public async Task<IActionResult> GetList([FromQuery] GetRateAgreementsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<RateAgreementDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{id:guid}", Name = "GetRateAgreementById")]
    [ProducesResponseType(typeof(RateAgreementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Tariff.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetRateAgreementByIdQuery { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateRateAgreement")]
    [ProducesResponseType(typeof(RateAgreementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateRateAgreementCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetRateAgreementById", new { id = result.Value!.Id }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}", Name = "UpdateRateAgreement")]
    [ProducesResponseType(typeof(RateAgreementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRateAgreementCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/approve", Name = "ApproveRateAgreement")]
    [ProducesResponseType(typeof(RateAgreementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ApproveRateAgreementCommand { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/revise", Name = "ReviseRateAgreement")]
    [ProducesResponseType(typeof(RateAgreementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> Revise(Guid id, [FromBody] ReviseRateAgreementCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetRateAgreementById", new { id = result.Value!.Id }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("costing", Name = "CostRoute")]
    [ProducesResponseType(typeof(RouteCostingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tariff.View)]
    public async Task<IActionResult> CostRoute([FromQuery] CostRouteQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}/billing-rules", Name = "SetRateAgreementBillingRules")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> SetBillingRules(
        Guid id, [FromBody] SetRateAgreementBillingRulesCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/withdraw", Name = "WithdrawRateAgreement")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new WithdrawRateAgreementCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }
}
