using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.Operations.RateAgreements.Commands;
using Trackwise.Application.Modules.Operations.RateAgreements.Queries;
using Trackwise.Application.Modules.Operations.Trips.Commands;
using Trackwise.Application.Modules.Operations.Trips.Queries;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("trips")]
[Produces("application/json")]
public class TripController(IMediator mediator) : ControllerBase
{
    private const int MaxPhotoUploadBytes = 50 * 1024 * 1024;

    [HttpGet("{tripId:guid}", Name = "GetTripById")]
    [ProducesResponseType(typeof(TripDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Load.View)]
    public async Task<IActionResult> GetById(Guid tripId)
    {
        var result = await mediator.Send(new GetTripQuery { TripId = tripId });
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetTrips")]
    [ProducesResponseType(typeof(PagedResponse<TripDto>), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.Load.View)]
    public async Task<IActionResult> GetList([FromQuery] GetTripsQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(PagedResponse<TripDto>.FromPagedResult(result, query.Page, query.PageSize));
    }

    [HttpPost(Name = "CreateTrip")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> CreateTrip([FromBody] CreateTripCommand request)
    {
        var result = await mediator.Send(request);
        return result.IsSuccess ? Ok(result) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("optimize", Name = "OptimizeTripStops")]
    [ProducesResponseType(typeof(OptimizedTripStopsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.View)]
    public async Task<IActionResult> OptimizeTripStops([FromBody] OptimizeTripStopsCommand request)
    {
        var result = await mediator.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}", Name = "UpdateTrip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> UpdateTrip(Guid id, [FromBody] UpdateTripCommand request)
    {
        request.TripId = id;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("{id:guid}", Name = "DeleteTrip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> DeleteTrip(Guid id)
    {
        var result = await mediator.Send(new DeleteTripCommand { Id = id });
        return result.IsSuccess ? NoContent() : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/dispatch", Name = "DispatchTrip")]
    [ProducesResponseType(typeof(DispatchTripResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> DispatchTrip(Guid tripId)
    {
        var result = await mediator.Send(new DispatchTripCommand { TripId = tripId });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/cancel", Name = "CancelTrip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> CancelTrip(Guid tripId, [FromBody] CancelTripCommand? request = null)
    {
        var command = request ?? new CancelTripCommand();
        command.TripId = tripId;
        var result = await mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/stops/{stopId:guid}/arrive", Name = "MarkStopArrived")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> MarkStopArrived(
        Guid tripId, Guid stopId, [FromBody] MarkStopArrivedCommand? request = null)
    {
        var command = request ?? new MarkStopArrivedCommand();
        command.TripId = tripId;
        command.StopId = stopId;
        var result = await mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/stops/{stopId:guid}/depart", Name = "MarkStopDeparted")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> MarkStopDeparted(
        Guid tripId, Guid stopId, [FromBody] MarkStopDepartedCommand request)
    {
        request.TripId = tripId;
        request.StopId = stopId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/garage-times", Name = "RecordTripGarageTimes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> RecordTripGarageTimes(Guid tripId, [FromBody] RecordTripGarageTimesCommand request)
    {
        request.TripId = tripId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/report", Name = "RecordTripReport")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> RecordTripReport(Guid tripId, [FromBody] RecordTripReportCommand request)
    {
        request.TripId = tripId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/stops/{stopId:guid}/correction", Name = "CorrectTripStop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> CorrectTripStop(Guid tripId, Guid stopId, [FromBody] CorrectTripStopCommand request)
    {
        request.TripId = tripId;
        request.StopId = stopId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/garage-times/correction", Name = "CorrectTripGarageTimes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> CorrectTripGarageTimes(Guid tripId, [FromBody] CorrectTripGarageTimesCommand request)
    {
        request.TripId = tripId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/rescues", Name = "RecordTripRescue")]
    [ProducesResponseType(typeof(TripRescueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> RecordTripRescue(Guid tripId, [FromBody] RecordTripRescueCommand request)
    {
        request.TripId = tripId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("{tripId:guid}/rescues/{rescueId:guid}", Name = "UndoTripRescue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> UndoTripRescue(Guid tripId, Guid rescueId)
    {
        var result = await mediator.Send(new UndoTripRescueCommand { TripId = tripId, RescueId = rescueId });
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{tripId:guid}/rescues/{rescueId:guid}/garage-times", Name = "RecordTripRescueGarageTimes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> RecordTripRescueGarageTimes(
        Guid tripId, Guid rescueId, [FromBody] RecordTripRescueGarageTimesCommand request)
    {
        request.TripId = tripId;
        request.RescueId = rescueId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{tripId:guid}/corrections", Name = "GetTripCorrections")]
    [ProducesResponseType(typeof(List<TripCorrectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.View)]
    public async Task<IActionResult> GetTripCorrections(Guid tripId)
    {
        var result = await mediator.Send(new GetTripCorrectionsQuery { TripId = tripId });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{tripId:guid}/photos", Name = "GetTripStopPhotos")]
    [ProducesResponseType(typeof(List<TripStopPhotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.View)]
    public async Task<IActionResult> GetTripStopPhotos(Guid tripId)
    {
        var result = await mediator.Send(new GetTripStopPhotosQuery { TripId = tripId });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/photos", Name = "AddTripStopPhotos")]
    [ProducesResponseType(typeof(List<TripStopPhotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    [RequestSizeLimit(MaxPhotoUploadBytes)]
    public async Task<IActionResult> AddTripStopPhotos(Guid tripId, [FromForm] AddTripStopPhotosRequest request)
    {
        var command = new AddTripStopPhotosCommand
        {
            TripId = tripId,
            StopIds = request.StopIds,
            Photos = request.Photos
                .Where(p => p.Length > 0)
                .Select(p => new FileUpload
                {
                    Content = p.OpenReadStream(),
                    FileName = p.FileName,
                    ContentType = p.ContentType,
                    FileSizeBytes = p.Length
                })
                .ToList()
        };

        var result = await mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("{tripId:guid}/photos/{photoId:guid}", Name = "DeleteTripStopPhoto")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> DeleteTripStopPhoto(Guid tripId, Guid photoId)
    {
        var result = await mediator.Send(new DeleteTripStopPhotoCommand { TripId = tripId, PhotoId = photoId });
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{tripId:guid}/rate-quote", Name = "GetTripRateQuote")]
    [ProducesResponseType(typeof(TripRateQuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.View)]
    public async Task<IActionResult> GetTripRateQuote(Guid tripId, [FromQuery] DateOnly? ratedOn)
    {
        var result = await mediator.Send(new GetTripRateQuoteQuery { TripId = tripId, RatedOn = ratedOn });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{tripId:guid}/price", Name = "PriceTrip")]
    [ProducesResponseType(typeof(TripRateQuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Load.Manage)]
    public async Task<IActionResult> PriceTrip(Guid tripId, [FromBody] PriceTripCommand? request = null)
    {
        var command = request ?? new PriceTripCommand();
        command.TripId = tripId;
        var result = await mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{tripId:guid}/timeline", Name = "GetTripTimeline")]
    [ProducesResponseType(typeof(TripTimelineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Load.View)]
    public async Task<IActionResult> GetTripTimeline(Guid tripId)
    {
        var result = await mediator.Send(new GetTripTimelineQuery { TripId = tripId });
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }
}

public record AddTripStopPhotosRequest(List<Guid> StopIds, List<IFormFile> Photos);
