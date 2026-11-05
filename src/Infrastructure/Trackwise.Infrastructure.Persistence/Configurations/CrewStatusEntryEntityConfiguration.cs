using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class CrewStatusEntryEntityConfiguration : IEntityTypeConfiguration<CrewStatusEntry>
{
    public void Configure(EntityTypeBuilder<CrewStatusEntry> builder)
    {
        builder.ToTable("crew_status_entries", t => t.HasCheckConstraint(
            "ck_crew_status_entries_one_person",
            "(employee_id IS NULL) <> (helper_id IS NULL)"));

        builder.Ignore(i => i.PersonId);
        builder.Property(i => i.Notes).HasMaxLength(500);

        builder.HasOne(i => i.Employee)
            .WithMany()
            .HasForeignKey(i => i.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Helper)
            .WithMany()
            .HasForeignKey(i => i.HelperId)
            .OnDelete(DeleteBehavior.Cascade);

        // One status per person per day. Postgres lets the null side of each index repeat freely.
        builder.HasIndex(i => new { i.Date, i.EmployeeId }).IsUnique();
        builder.HasIndex(i => new { i.Date, i.HelperId }).IsUnique();
    }
}
