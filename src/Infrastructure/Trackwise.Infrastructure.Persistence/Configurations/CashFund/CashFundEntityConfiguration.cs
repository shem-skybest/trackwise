using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class CashFundEntityConfiguration : IEntityTypeConfiguration<CashFund>
{
    public void Configure(EntityTypeBuilder<CashFund> builder)
    {
        builder.ToTable("cash_funds");

        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.Property(i => i.AdvanceReference).HasMaxLength(64);
        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.Property(i => i.LiquidatedBy).HasMaxLength(200);

        builder.HasOne(i => i.Custodian)
            .WithMany()
            .HasForeignKey(i => i.CustodianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Expenses)
            .WithOne()
            .HasForeignKey(i => i.CashFundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.CustodianId, i.PeriodStart });
        builder.HasIndex(i => i.Status);
    }
}
