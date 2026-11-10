using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Platform.Contacts.Commands;
using Trackwise.Domain.Entities.Note;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Notes.Commands.CreateNote;

internal sealed class CreateNoteHandler(
    ITenantUnitOfWork uow,
    ILogger<CreateNoteHandler> logger) : IAppRequestHandler<CreateNoteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateNoteCommand req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
        {
            logger.LogWarning("Failed to create note: Content is empty");
            return Result<Guid>.Fail(ErrorCodes.Validation, "Note content cannot be empty.");
        }

        var note = Note.Create(req.Title, req.Content, req.UserId);

        await uow.Repository<Note>().AddAsync(note, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Created note with ID {NoteId}", note.Id);

        return Result<Guid>.Ok(note.Id);
    }
}
