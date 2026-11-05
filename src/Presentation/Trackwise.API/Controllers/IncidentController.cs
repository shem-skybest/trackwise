using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.Compliance.Incidents.Commands;
using Trackwise.Application.Modules.Compliance.Incidents.Queries;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("incidents")]
[Produces("application/json")]
public class IncidentController(IMediator mediator) : ControllerBase
{
    private const int MaxAttachmentUploadBytes = 60 * 1024 * 1024;

    [HttpGet(Name = "GetIncidents")]
    [ProducesResponseType(typeof(PagedResponse<IncidentDto>), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.Safety.View)]
    public async Task<IActionResult> GetIncidents([FromQuery] GetIncidentsQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(PagedResponse<IncidentDto>.FromPagedResult(result, query.Page, query.PageSize));
    }

    [HttpGet("{id:guid}", Name = "GetIncidentById")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Safety.View)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await mediator.Send(new GetIncidentByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateIncident")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Safety.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateIncidentCommand request)
    {
        var result = await mediator.Send(request);
        return result.IsSuccess
            ? CreatedAtRoute("GetIncidentById", new { id = result.Value!.Id }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}", Name = "UpdateIncident")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Safety.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIncidentCommand request)
    {
        request.Id = id;
        return ToResponse(await mediator.Send(request));
    }

    [HttpDelete("{id:guid}", Name = "DeleteIncident")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Safety.Manage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await mediator.Send(new DeleteIncidentCommand { Id = id });
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/review", Name = "ReviewIncident")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Safety.Manage)]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewIncidentCommand request)
    {
        request.Id = id;
        return ToResponse(await mediator.Send(request));
    }

    [HttpPost("{id:guid}/resolve", Name = "ResolveIncident")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Safety.Manage)]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveIncidentCommand request)
    {
        request.Id = id;
        return ToResponse(await mediator.Send(request));
    }

    [HttpPost("{id:guid}/reopen", Name = "ReopenIncident")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Safety.Manage)]
    public async Task<IActionResult> Reopen(Guid id, [FromBody] ReopenIncidentCommand request)
    {
        request.Id = id;
        return ToResponse(await mediator.Send(request));
    }

    [HttpPost("{id:guid}/close", Name = "CloseIncident")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Safety.Manage)]
    public async Task<IActionResult> Close(Guid id)
    {
        return ToResponse(await mediator.Send(new CloseIncidentCommand { Id = id }));
    }

    [HttpPut("{id:guid}/repairs", Name = "SetIncidentRepairs")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Safety.Manage)]
    public async Task<IActionResult> SetRepairs(Guid id, [FromBody] SetIncidentRepairsCommand request)
    {
        request.Id = id;
        return ToResponse(await mediator.Send(request));
    }

    [HttpPost("{id:guid}/attachments", Name = "AddIncidentAttachments")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Safety.Manage)]
    [RequestSizeLimit(MaxAttachmentUploadBytes)]
    public async Task<IActionResult> AddAttachments(Guid id, [FromForm] AddIncidentAttachmentsRequest request)
    {
        var command = new AddIncidentAttachmentsCommand
        {
            IncidentId = id,
            Type = request.Type,
            Files = request.Files
                .Where(f => f.Length > 0)
                .Select(f => new FileUpload
                {
                    Content = f.OpenReadStream(),
                    FileName = f.FileName,
                    ContentType = f.ContentType,
                    FileSizeBytes = f.Length
                })
                .ToList()
        };

        return ToResponse(await mediator.Send(command));
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}", Name = "DeleteIncidentAttachment")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Safety.Manage)]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    {
        return ToResponse(await mediator.Send(
            new DeleteIncidentAttachmentCommand { IncidentId = id, AttachmentId = attachmentId }));
    }

    private IActionResult ToResponse(Result<IncidentDto> result) =>
        result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
}

public record AddIncidentAttachmentsRequest(DocumentType Type, List<IFormFile> Files);
