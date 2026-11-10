using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Note;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Notes;

namespace Trackwise.Application.Modules.Platform.Notes.Queries.GetNoteById;

internal sealed class GetNoteByIdHandler(
    ITenantUnitOfWork uow,
    ILogger<GetNoteByIdHandler> logger) : IAppRequestHandler<GetNoteByIdQuery, Result<NoteDto>>
{
    public async Task<Result<NoteDto>> Handle(GetNoteByIdQuery req, CancellationToken ct)
    {
        var note = await uow.Repository<Note>()
            .Query()
            .AsNoTracking()
            .Where(n => n.Id == req.Id)
            .Select(n => new NoteDto(
                n.Id,
                n.Title,
                n.Content,
                n.DisplayOrder,
                n.IsPinned,
                n.CreatedAt,
                n.CreatedBy,
                n.UpdatedAt,
                n.UpdatedBy))
            .FirstOrDefaultAsync(ct);

        if (note is null)
        {
            logger.LogWarning("Note with ID {NoteId} was not found", req.Id);
            return Result<NoteDto>.Fail(ErrorCodes.NotFound, $"Note with ID '{req.Id}' was not found.");
        }

        return Result<NoteDto>.Ok(note);
    }
}
