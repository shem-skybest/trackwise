using Trackwise.Domain.Entities.Safety;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class IncidentThirdPartyEntityConfiguration : IEntityTypeConfiguration<IncidentThirdParty>
{
    public void Configure(EntityTypeBuilder<IncidentThirdParty> builder)
    {
        builder.ToTable("incident_third_parties");

        builder.HasIndex(i => i.IncidentId);

        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.PhoneNumber).HasMaxLength(50);
        builder.Property(i => i.Address).HasMaxLength(500);
        builder.Property(i => i.DriverLicense).HasMaxLength(50);
        builder.Property(i => i.VehicleMake).HasMaxLength(100);
        builder.Property(i => i.VehicleModel).HasMaxLength(100);
        builder.Property(i => i.VehicleLicensePlate).HasMaxLength(20);
        builder.Property(i => i.VehicleVin).HasMaxLength(20);
        builder.Property(i => i.VehicleColor).HasMaxLength(50);
        builder.Property(i => i.InsuranceCompany).HasMaxLength(200);
        builder.Property(i => i.InsurancePolicyNumber).HasMaxLength(100);
        builder.Property(i => i.InsuranceAgentPhone).HasMaxLength(50);
    }
}
