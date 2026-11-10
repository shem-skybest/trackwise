using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Entities.Note;

namespace Trackwise.Infrastructure.Persistence.Configurations.Notes;

internal sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(n => n.Content)
            .IsRequired();

        builder.Property(n => n.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(n => n.IsPinned)
            .HasDefaultValue(false);

        // AuditableEntity properties
        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.CreatedBy)
            .HasMaxLength(100);

        builder.Property(n => n.UpdatedAt)
            .IsRequired(false);

        builder.Property(n => n.UpdatedBy)
            .HasMaxLength(100);

        // Indexes for performance when rendering desktop boards
        builder.HasIndex(n => new { n.IsPinned, n.DisplayOrder });
        builder.HasIndex(n => n.CreatedAt);

        // Ignore domain events from persistence mapping
        builder.Ignore(n => n.DomainEvents);
    }
}
