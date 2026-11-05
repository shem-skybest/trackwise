using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.AddBookingCharge;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.CancelBooking;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.ConfirmBooking;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.GenerateBookingPdf;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.RemoveBookingCharge;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.GenerateQuotationPdf;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("bookings")]
[Produces("application/json")]
public class BookingController(IMediator mediator) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet("{id:guid}", Name = "GetBookingById")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBookingByIdQuery { Id = id }, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetBookings")]
    [ProducesResponseType(typeof(PagedResponse<BookingSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.View)]
    public async Task<IActionResult> GetList([FromQuery] GetBookingsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<BookingSummaryDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>This year's BKN-RFV counter and the number the next Cold Chain booking will get.</summary>
    [HttpGet("numbering", Name = "GetBookingNumbering")]
    [ProducesResponseType(typeof(BookingNumberingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tenant.View)]
    public async Task<IActionResult> GetNumbering(CancellationToken ct)
    {
        var result = await mediator.Send(new GetBookingNumberingQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Sets the number the next Cold Chain booking gets this year - run once at cut-over so BKN-RFV
    /// continues from nSFS. Refuses a number that has already been issued.
    /// </summary>
    [HttpPut("numbering", Name = "SetNextBookingNumber")]
    [ProducesResponseType(typeof(BookingNumberingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tenant.Manage)]
    public async Task<IActionResult> SetNextNumber([FromBody] SetNextBookingNumberCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Switches Cold Chain booking numbers between typed in from nSFS (while it still issues them)
    /// and issued by TrackWise.
    /// </summary>
    [HttpPut("numbering/typed", Name = "SetBookingNumbersTyped")]
    [ProducesResponseType(typeof(BookingNumberingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tenant.Manage)]
    public async Task<IActionResult> SetNumbersTyped([FromBody] SetBookingNumbersTypedCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Types in the BKN-RFV number nSFS gave a Cold Chain booking; returns it as stored.</summary>
    [HttpPut("{id:guid}/number", Name = "SetBookingReferenceNumber")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> SetReferenceNumber(
        Guid id, [FromBody] SetBookingReferenceNumberCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>One day's Cold Chain bookings with no trip yet: confirmed ones first, then pending.</summary>
    [HttpGet("unassigned", Name = "GetUnassignedBookings")]
    [ProducesResponseType(typeof(List<UnassignedBookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.View)]
    public async Task<IActionResult> GetUnassigned([FromQuery] GetUnassignedBookingsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Books one Cold Chain truck-trip from the client's rate agreement.</summary>
    [HttpPost(Name = "CreateColdChainBooking")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> CreateColdChain([FromBody] CreateColdChainBookingCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetBookingById", new { id = result.Value }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}", Name = "UpdateColdChainBooking")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> UpdateColdChain(
        Guid id, [FromBody] UpdateColdChainBookingCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Prices a Cold Chain booking plan without saving it, for the booking form.</summary>
    [HttpPost("estimate", Name = "EstimateColdChainBooking")]
    [ProducesResponseType(typeof(BookingEstimateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> EstimateColdChain(
        [FromBody] EstimateColdChainBookingQuery request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Copies a Cold Chain booking onto each chosen day - a standing order.</summary>
    [HttpPost("{id:guid}/repeat", Name = "RepeatColdChainBooking")]
    [ProducesResponseType(typeof(List<RepeatedBookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> Repeat(
        Guid id, [FromBody] RepeatColdChainBookingRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new RepeatColdChainBookingCommand
        {
            BookingId = id,
            Dates = request.Dates,
            Confirm = request.Confirm,
            ConfirmedBy = UserId
        }, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>Makes the trip for a confirmed Cold Chain booking and prices it.</summary>
    [HttpPost("{id:guid}/trip", Name = "CreateTripFromBooking")]
    [ProducesResponseType(typeof(CreateTripFromBookingResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> CreateTrip(
        Guid id, [FromBody] CreateTripFromBookingRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(UserId, out var dispatcherId))
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new CreateTripFromBookingCommand
        {
            BookingId = id,
            TruckId = request.TruckId,
            DriverId = request.DriverId,
            HelperId = request.HelperId,
            SecondHelperId = request.SecondHelperId,
            DispatcherId = dispatcherId
        }, ct);

        return result.IsSuccess
            ? CreatedAtRoute("GetTripById", new { tripId = result.Value!.TripId }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/confirm", Name = "ConfirmBooking")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ConfirmBookingCommand
        {
            Id = id,
            ConfirmedBy = UserId
        }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/start", Name = "StartBookingExecution")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> StartExecution(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new StartBookingExecutionCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/complete", Name = "CompleteBooking")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new CompleteBookingCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Promotes a Confirmed booking into a workfile. Lives here rather than on WorkfileController
    /// because creation-by-conversion belongs to the source aggregate - the same shape as
    /// <c>QuotationController.ConvertToBooking</c>.
    /// </summary>
    [HttpPost("{id:guid}/promote-to-workfile", Name = "PromoteBookingToWorkfile")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Workfile.Manage)]
    public async Task<IActionResult> PromoteToWorkfile(
        Guid id, [FromBody] PromoteBookingToWorkfileRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new PromoteBookingToWorkfileCommand
        {
            BookingId = id,
            Type = request.Type,
            Direction = request.Direction,
            Variant = request.Variant,
            ReportCategory = request.ReportCategory,
            ReferenceNumber = request.ReferenceNumber,
            BillOfLadingNumber = request.BillOfLadingNumber,
            BillIndicator = request.BillIndicator,
            VesselName = request.VesselName,
            VoyageNumber = request.VoyageNumber,
            Etd = request.Etd,
            Eta = request.Eta,
            Origin = request.Origin,
            Destination = request.Destination,
            Commodity = request.Commodity,
            ShippingLineId = request.ShippingLineId,
            CoLoaderId = request.CoLoaderId,
            ForwarderId = request.ForwarderId,
            TruckerId = request.TruckerId,
            BrokerId = request.BrokerId,
            CreditTermDays = request.CreditTermDays,
            CreditType = request.CreditType,
            ExchangeRate = request.ExchangeRate,
            TaggingCategory = request.TaggingCategory,
            IsAdvanceBilling = request.IsAdvanceBilling
        }, ct);

        return result.IsSuccess
            ? CreatedAtRoute("GetWorkfileById", new { id = result.Value }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/operational-status", Name = "SetBookingOperationalStatus")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> SetOperationalStatus(
        Guid id, [FromBody] SetBookingOperationalStatusCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/cancel", Name = "CancelBooking")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelBookingRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelBookingCommand
        {
            Id = id,
            CancelledBy = UserId,
            Reason = request.Reason,
            CancelledByParty = request.CancelledByParty,
            LossReason = request.LossReason
        }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Generates the booking confirmation as a PDF. The default is the customer-facing document.
    /// <c>includeInternalCosts=true</c> adds buying cost and margin and needs the cost permission.
    /// </summary>
    /// <param name="id">The booking id.</param>
    /// <param name="includeInternalCosts">Internal copy with buying cost and margin. Requires <see cref="Permission.Booking.ViewCosts"/>.</param>
    [HttpGet("{id:guid}/pdf", Name = "GenerateBookingPdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]   // customer and pricing data: never cache
    [Authorize(Policy = Permission.Booking.View)]
    public async Task<IActionResult> GeneratePdf(
        Guid id,
        [FromQuery] bool includeInternalCosts,
        [FromServices] IAuthorizationService authorization,
        CancellationToken ct)
    {
        if (includeInternalCosts)
        {
            var allowed = await authorization.AuthorizeAsync(User, Permission.Booking.ViewCosts);
            if (!allowed.Succeeded)
            {
                return Forbid();
            }
        }

        var result = await mediator.Send(new GenerateBookingPdfCommand(id, includeInternalCosts), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == ErrorCodes.NotFound
                ? NotFound(ErrorResponse.FromResult(result))
                : StatusCode(StatusCodes.Status500InternalServerError, ErrorResponse.FromResult(result));
        }

        return File(result.Value!.Content, "application/pdf", result.Value.FileName);
    }

    /// <summary>
    /// Adds a charge that was not in the quotation (detention, extra handling, a discount...).
    /// The charge is Unbilled and is picked up by the next invoice.
    /// </summary>
    [HttpPost("{id:guid}/charges", Name = "AddBookingCharge")]
    [ProducesResponseType(typeof(BookingChargeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> AddCharge(
        Guid id, [FromBody] AddAdditionalChargeRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new AddBookingChargeCommand(
            id, request.Category, request.Description, request.Quantity,
            request.UnitBuying, request.UnitSelling, request.Reason), ct);

        return ToActionResult(result);
    }

    /// <summary>Removes an additional charge that has not been invoiced. Quoted charges cannot be removed.</summary>
    [HttpDelete("{id:guid}/charges/{chargeId:guid}", Name = "RemoveBookingCharge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Booking.Manage)]
    public async Task<IActionResult> RemoveCharge(Guid id, Guid chargeId, CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveBookingChargeCommand(id, chargeId), ct);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(ErrorResponse.FromResult(result))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    // Use your existing Result -> IActionResult helper if you have one; this is the same mapping inline.
    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(ErrorResponse.FromResult(result))
            : BadRequest(ErrorResponse.FromResult(result));
    }
}
