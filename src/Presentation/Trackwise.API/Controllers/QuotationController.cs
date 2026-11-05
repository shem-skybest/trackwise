using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.AcceptQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.AddQuotationLine;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ApproveQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ConvertQuotationToBooking;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.GenerateQuotationPdf;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ExpireQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.IssueQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.RejectQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.RemoveQuotationLine;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.SubmitQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.UpdateQuotation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("quotations")]
[Produces("application/json")]
public class QuotationController(IMediator mediator) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet("{id:guid}", Name = "GetQuotationById")]
    [ProducesResponseType(typeof(QuotationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetQuotationByIdQuery(id), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetQuotations")]
    [ProducesResponseType(typeof(PagedResponse<QuotationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.View)]
    public async Task<IActionResult> GetList([FromQuery] GetQuotationsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<QuotationSummaryDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateQuotation")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateQuotationCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetQuotationById", new { id = result.Value }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}", Name = "UpdateQuotation")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Quotation.Manage)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] CreateQuotationCommand request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateQuotationCommand(id, request), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(ErrorResponse.FromResult(result))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/lines", Name = "AddQuotationLine")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.Manage)]
    public async Task<IActionResult> AddLine(Guid id, [FromBody] AddQuotationLineCommand request, CancellationToken ct)
    {
        request.QuotationId = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetQuotationById", new { id }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}", Name = "RemoveQuotationLine")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.Manage)]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken ct)
    {
        var command = new RemoveQuotationLineCommand(id, lineId);
        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/submit", Name = "SubmitQuotation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.Manage)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new SubmitQuotationCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/approve", Name = "ApproveQuotation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.Manage)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ApproveQuotationCommand { Id = id, ApprovedBy = UserId }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/reject", Name = "RejectQuotation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.Manage)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectQuotationRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new RejectQuotationCommand
        {
            Id = id,
            RejectedBy = UserId,
            Reason = request.Reason
        }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/issue", Name = "IssueQuotation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.Manage)]
    public async Task<IActionResult> Issue(Guid id, [FromBody] IssueQuotationRequest? request, CancellationToken ct)
    {
        var result = await mediator.Send(new IssueQuotationCommand
        {
            Id = id,
            ValidityDays = request?.ValidityDays ?? 14
        }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/accept", Name = "AcceptQuotation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.Manage)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new AcceptQuotationCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/expire", Name = "ExpireQuotation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Quotation.Manage)]
    public async Task<IActionResult> Expire(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ExpireQuotationCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/convert-to-booking", Name = "ConvertQuotationToBooking")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> ConvertToBooking(Guid id, [FromBody] ConvertQuotationToBookingRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new ConvertQuotationToBookingCommand
        {
            QuotationId = id,
            ContainerTypeId = request.ContainerTypeId,
            RequestedPickupDate = request.RequestedPickupDate,
            RequestedDeliveryDate = request.RequestedDeliveryDate,
            Notes = request.Notes
        }, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetBookingById", new { id = result.Value }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Generates a customer-facing formal rate offer (quotation) as a PDF.
    /// </summary>
    [HttpGet("{id:guid}/pdf", Name = "GenerateQuotationPdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Quotation.View)]
    public async Task<IActionResult> GeneratePdf(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GenerateQuotationPdfCommand(id), ct);

        if (!result.IsSuccess)
        {
            return NotFound(ErrorResponse.FromResult(result));
        }

        return File(result.Value!.Content, "application/pdf", result.Value.FileName);
    }

}


public record RejectQuotationRequest(string Reason);
public record IssueQuotationRequest(int ValidityDays = 14);
