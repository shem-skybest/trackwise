using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities.Note;

public class Note : AuditableEntity, ITenantEntity
{
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPinned { get; set; } = false;

    // Parameterless constructor for EF Core
    protected Note() { }

    // Factory method
    public static Note Create(
        string? title,
        string content,
        string? userId = null)
    {
        var note = new Note
        {
            Title = title,
            Content = content
        };

        note.SetCreated(userId);
        note.DomainEvents.Add(new NoteCreatedEvent(note.Id));

        return note;
    }

    // Business behaviors raising domain events
    public void UpdateContent(string? title, string content, string? userId = null)
    {
        Title = title;
        Content = content;

        SetUpdated(userId);
        DomainEvents.Add(new NoteContentUpdatedEvent(Id));
    }

    public void Delete()
    {
        DomainEvents.Add(new NoteDeletedEvent(Id));
    }

    public void TogglePin(string? userId = null)
    {
        IsPinned = !IsPinned;
        SetUpdated(userId);

        if (IsPinned)
            DomainEvents.Add(new NotePinnedEvent(Id));
        else
            DomainEvents.Add(new NoteUnpinnedEvent(Id));
    }

    public sealed record NoteCreatedEvent(
    Guid NoteId
) : IDomainEvent;

    public sealed record NoteContentUpdatedEvent(
        Guid NoteId
    ) : IDomainEvent;

    public sealed record NotePinnedEvent(
        Guid NoteId
    ) : IDomainEvent;

    public sealed record NoteUnpinnedEvent(
        Guid NoteId
    ) : IDomainEvent;

    public sealed record NoteDeletedEvent(
        Guid NoteId
    ) : IDomainEvent;
}
