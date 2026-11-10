using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Note;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Notes.Commands.ToggleNotePin;

internal sealed class ToggleNotePinHandler(
    ITenantUnitOfWork uow,
    ILogger<ToggleNotePinHandler> logger) : IAppRequestHandler<ToggleNotePinCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ToggleNotePinCommand req, CancellationToken ct)
    {
        var repo = uow.Repository<Note>();
        var note = await repo.GetByIdAsync(req.Id, ct);

        if (note is null)
        {
            logger.LogWarning("Note with ID {NoteId} not found for pin toggle", req.Id);
            return Result<bool>.Fail(ErrorCodes.NotFound, $"Note with ID '{req.Id}' was not found.");
        }

        note.TogglePin(req.UserId);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Toggled pin state for note {NoteId} to {IsPinned}", note.Id, note.IsPinned);

        return Result<bool>.Ok(note.IsPinned);
    }
}
