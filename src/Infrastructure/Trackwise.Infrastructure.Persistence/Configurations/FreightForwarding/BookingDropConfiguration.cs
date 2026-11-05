using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class BookingDropConfiguration : IEntityTypeConfiguration<BookingDrop>
{
    public void Configure(EntityTypeBuilder<BookingDrop> builder)
    {
        builder.ToTable("booking_drops");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => x.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BookingId, x.SortOrder });
    }
}
