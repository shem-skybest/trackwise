using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trackwise.Application.Modules.Platform.Notes.Commands.CreateNote;
using Trackwise.Application.Modules.Platform.Notes.Commands.DeleteNote;
using Trackwise.Application.Modules.Platform.Notes.Commands.ReOrderNote;
using Trackwise.Application.Modules.Platform.Notes.Commands.ToggleNotePin;
using Trackwise.Application.Modules.Platform.Notes.Commands.UpdateNote;
using Trackwise.Application.Modules.Platform.Notes.Queries.GetNoteById;
using Trackwise.Application.Modules.Platform.Notes.Queries.GetNotes;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Notes;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("notes")]
[Produces("application/json")]
public class NoteController(IMediator mediator) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet("{id:guid}", Name = "GetNoteById")]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[Authorize(Policy = Permission.Note.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetNoteByIdQuery(id), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetNotes")]
    [ProducesResponseType(typeof(PagedResponse<NoteSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[Authorize(Policy = Permission.Note.View)]
    public async Task<IActionResult> GetList([FromQuery] GetNotesQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<NoteSummaryDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateNote")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[Authorize(Policy = Permission.Note.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateNoteRequest request, CancellationToken ct)
    {
        var command = new CreateNoteCommand(request.Title, request.Content, UserId);
        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtRoute("GetNoteById", new { id = result.Value }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}", Name = "UpdateNote")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[Authorize(Policy = Permission.Note.Manage)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateNoteRequest request,
        CancellationToken ct)
    {
        var command = new UpdateNoteCommand(id, request.Title, request.Content, UserId);
        var result = await mediator.Send(command, ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(ErrorResponse.FromResult(result))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPatch("{id:guid}/toggle-pin", Name = "ToggleNotePin")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[Authorize(Policy = Permission.Note.Manage)]
    public async Task<IActionResult> TogglePin(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ToggleNotePinCommand(id, UserId), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(ErrorResponse.FromResult(result))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPatch("reorder", Name = "ReorderNotes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[Authorize(Policy = Permission.Note.Manage)]
    public async Task<IActionResult> Reorder([FromBody] ReorderNotesRequest request, CancellationToken ct)
    {
        var command = new ReorderNotesCommand(request.Items, UserId);
        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("{id:guid}", Name = "DeleteNote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[Authorize(Policy = Permission.Note.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteNoteCommand(id), ct);

        if (result.IsSuccess)
            return NoContent();

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(ErrorResponse.FromResult(result))
            : BadRequest(ErrorResponse.FromResult(result));
    }
}
