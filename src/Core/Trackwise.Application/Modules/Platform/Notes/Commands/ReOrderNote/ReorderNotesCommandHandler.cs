using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Note;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Notes.Commands.ReOrderNote;

internal sealed class ReorderNotesHandler(
    ITenantUnitOfWork uow,
    ILogger<ReorderNotesHandler> logger) : IAppRequestHandler<ReorderNotesCommand, Result>
{
    public async Task<Result> Handle(ReorderNotesCommand req, CancellationToken ct)
    {
        var requestedIds = req.Items.Select(x => x.Id).ToList();
        var notes = await uow.Repository<Note>()
            .Query()
            .Where(n => requestedIds.Contains(n.Id))
            .ToListAsync(ct);

        var orderLookup = req.Items.ToDictionary(x => x.Id, x => x.DisplayOrder);

        foreach (var note in notes)
        {
            if (orderLookup.TryGetValue(note.Id, out var newOrder))
            {
                note.DisplayOrder = newOrder;
                note.SetUpdated(req.UserId);
            }
        }

        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Successfully updated display order for {Count} notes", notes.Count);

        return Result.Ok();
    }
}
