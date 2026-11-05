using Trackwise.Application.Modules.Financial.BillingBatches.Commands;
using Trackwise.Application.Modules.Financial.BillingBatches.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers;

/// <summary>
/// Billing batches: one client's billing period, prepared by Ops and forwarded to Accounting. Guarded
/// by the invoice permissions - viewing needs <c>Invoice.View</c>, every change <c>Invoice.Manage</c>.
/// </summary>
[ApiController]
[Route("billing-batches")]
[Produces("application/json")]
public class BillingBatchController(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = "GetBillingBatches")]
    [ProducesResponseType(typeof(PagedResponse<BillingBatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.View)]
    public async Task<IActionResult> GetList([FromQuery] GetBillingBatchesQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<BillingBatchDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Per client, the completed trips no batch has taken yet.</summary>
    [HttpGet("unbilled", Name = "GetUnbilledTrips")]
    [ProducesResponseType(typeof(IReadOnlyList<UnbilledCustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.View)]
    public async Task<IActionResult> GetUnbilled(CancellationToken ct)
    {
        var result = await mediator.Send(new GetUnbilledTripsQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{id:guid}", Name = "GetBillingBatchById")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Invoice.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBillingBatchByIdQuery { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    /// <summary>The printed summary for Accounting.</summary>
    [HttpGet("{id:guid}/pdf", Name = "GetBillingBatchPdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Invoice.View)]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBillingBatchPdfQuery { Id = id }, ct);
        return result.IsSuccess
            ? File(result.Value!.Content, "application/pdf", $"{result.Value.ReferenceNumber}.pdf")
            : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateBillingBatch")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateBillingBatchCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetBillingBatchById", new { id = result.Value!.Id }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/refresh", Name = "RefreshBillingBatch")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> Refresh(Guid id, CancellationToken ct) =>
        FromResult(await mediator.Send(new RefreshBillingBatchCommand { Id = id }, ct));

    [HttpDelete("{id:guid}/trips/{tripId:guid}", Name = "RemoveBillingBatchTrip")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> RemoveTrip(Guid id, Guid tripId, CancellationToken ct) =>
        FromResult(await mediator.Send(new RemoveBillingBatchTripCommand { Id = id, TripId = tripId }, ct));

    [HttpPost("{id:guid}/charges", Name = "AddBillingBatchCharge")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> AddCharge(Guid id, [FromBody] AddBillingBatchChargeCommand request, CancellationToken ct)
    {
        request.Id = id;
        return FromResult(await mediator.Send(request, ct));
    }

    /// <summary>Bills the trips' tolls, parking and passways from the cash fund receipts, at cost.</summary>
    [HttpPost("{id:guid}/charges/tolls", Name = "AddBillingBatchTollCharges")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> AddTollCharges(Guid id, CancellationToken ct) =>
        FromResult(await mediator.Send(new AddBillingBatchTollChargesCommand { Id = id }, ct));

    [HttpDelete("{id:guid}/charges/{chargeId:guid}", Name = "DeleteBillingBatchCharge")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> DeleteCharge(Guid id, Guid chargeId, CancellationToken ct) =>
        FromResult(await mediator.Send(new DeleteBillingBatchChargeCommand { Id = id, ChargeId = chargeId }, ct));

    [HttpPost("{id:guid}/lock", Name = "LockBillingBatch")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> Lock(Guid id, CancellationToken ct) =>
        FromResult(await mediator.Send(new LockBillingBatchCommand { Id = id }, ct));

    [HttpPost("{id:guid}/unlock", Name = "UnlockBillingBatch")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> Unlock(Guid id, [FromBody] UnlockBillingBatchCommand request, CancellationToken ct)
    {
        request.Id = id;
        return FromResult(await mediator.Send(request, ct));
    }

    [HttpPost("{id:guid}/forward", Name = "ForwardBillingBatch")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> Forward(Guid id, [FromBody] ForwardBillingBatchCommand request, CancellationToken ct)
    {
        request.Id = id;
        return FromResult(await mediator.Send(request, ct));
    }

    [HttpPost("{id:guid}/billed", Name = "MarkBillingBatchBilled")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> MarkBilled(Guid id, [FromBody] MarkBillingBatchBilledCommand request, CancellationToken ct)
    {
        request.Id = id;
        return FromResult(await mediator.Send(request, ct));
    }

    [HttpPost("{id:guid}/cancel", Name = "CancelBillingBatch")]
    [ProducesResponseType(typeof(BillingBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Invoice.Manage)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelBillingBatchCommand request, CancellationToken ct)
    {
        request.Id = id;
        return FromResult(await mediator.Send(request, ct));
    }

    private IActionResult FromResult(Result<BillingBatchDto> result) =>
        result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
}
