using Trackwise.Domain.Entities.Safety;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class IncidentWitnessEntityConfiguration : IEntityTypeConfiguration<IncidentWitness>
{
    public void Configure(EntityTypeBuilder<IncidentWitness> builder)
    {
        builder.ToTable("incident_witnesses");

        builder.HasIndex(i => i.IncidentId);

        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.PhoneNumber).HasMaxLength(50);
        builder.Property(i => i.Email).HasMaxLength(200);
        builder.Property(i => i.Address).HasMaxLength(500);
        builder.Property(i => i.Statement).HasMaxLength(4000);
    }
}
