using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Note;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Notes.Commands.DeleteNote;

internal sealed class DeleteNoteHandler(
    ITenantUnitOfWork uow,
    ILogger<DeleteNoteHandler> logger) : IAppRequestHandler<DeleteNoteCommand, Result>
{
    public async Task<Result> Handle(DeleteNoteCommand req, CancellationToken ct)
    {
        var repo = uow.Repository<Note>();
        var note = await repo.GetByIdAsync(req.Id, ct);

        if (note is null)
        {
            logger.LogWarning("Note with ID {NoteId} not found for deletion", req.Id);
            return Result.Fail(ErrorCodes.NotFound, $"Note with ID '{req.Id}' was not found.");
        }

        note.Delete();
        repo.Delete(note);

        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Deleted note with ID {NoteId}", req.Id);
        return Result.Ok();
    }
}
