using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Invoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceEntityConfiguration : IEntityTypeConfiguration<Invoice>
{
    // Invoice (TPH)
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasDiscriminator(i => i.Type)
            .HasValue<LoadInvoice>(InvoiceType.Load)
            .HasValue<SubscriptionInvoice>(InvoiceType.Subscription)
            .HasValue<PayrollInvoice>(InvoiceType.Payroll)
            .HasValue<BookingInvoice>(InvoiceType.Booking);

        builder.Property(i => i.Number)
            .UseIdentityAlwaysColumn()
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(i => i.Number)
            .IsUnique();

        builder.ComplexProperty(i => i.Subtotal, money =>
        {
            money.Property(m => m.Amount).HasPrecision(18, 2);
            money.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.ComplexProperty(i => i.TaxTotal, money =>
        {
            money.Property(m => m.Amount).HasPrecision(18, 2);
            money.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.ComplexProperty(i => i.Total, money =>
        {
            money.Property(m => m.Amount).HasPrecision(18, 2);
            money.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.Property(i => i.TaxBehavior)
            .HasDefaultValue(TaxBehavior.Exclusive);

        builder.Property(i => i.TaxBreakdownJson)
            .HasColumnType("text");
    }

    #region Derived Types Configuration

    // Fine-tune derived types for LoadInvoice, SubscriptionInvoice, and PayrollInvoice
    public sealed class LoadInvoiceEntityConfiguration : IEntityTypeConfiguration<LoadInvoice>
    {
        public void Configure(EntityTypeBuilder<LoadInvoice> builder)
        {
            builder.HasOne(i => i.Load)
                .WithOne(l => l.Invoice)
                .HasForeignKey<LoadInvoice>(i => i.LoadId)
                .IsRequired(false);
        }
    }

    public sealed class BookingInvoiceEntityConfiguration : IEntityTypeConfiguration<BookingInvoice>
    {
        public void Configure(EntityTypeBuilder<BookingInvoice> builder)
        {
            builder.HasOne(i => i.Booking)
                .WithMany()
                .HasForeignKey(i => i.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(i => i.Purpose)
                .HasDefaultValue(BookingInvoicePurpose.Original)
                .IsRequired();

            builder.Property(i => i.CancellationReason)
                .HasMaxLength(1000);

            builder.HasOne(i => i.RelatedInvoice)
                .WithMany()
                .HasForeignKey(i => i.RelatedInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(i => i.BookingId, "BookingInvoice_BookingId")
                .HasDatabaseName("ix_invoices_booking_id");
            builder.HasIndex(i => i.RelatedInvoiceId)
                .HasDatabaseName("ix_invoices_related_invoice_id");
            builder.HasIndex(i => i.BookingId, "BookingInvoice_OneDraftPerBooking")
                .IsUnique()
                .HasDatabaseName("ux_invoices_booking_id_draft")
                .HasFilter("\"booking_id\" IS NOT NULL AND \"status\" = 'draft'");
        }
    }

    public sealed class SubscriptionInvoiceEntityConfiguration : IEntityTypeConfiguration<SubscriptionInvoice>
    {
        public void Configure(EntityTypeBuilder<SubscriptionInvoice> builder)
        {
            // builder.HasOne(i => i.Subscription)
            //     .WithMany(s => s.Invoices)
            //     .HasForeignKey(i => i.SubscriptionId);
        }
    }

    public sealed class PayrollInvoiceEntityConfiguration : IEntityTypeConfiguration<PayrollInvoice>
    {
        public void Configure(EntityTypeBuilder<PayrollInvoice> builder)
        {
            builder.HasOne(i => i.Employee)
                .WithMany(e => e.PayrollInvoices)
                .HasForeignKey(i => i.EmployeeId);

            builder.HasOne(i => i.ApprovedBy)
                .WithMany()
                .HasForeignKey(i => i.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(i => i.TotalHoursWorked)
                .HasPrecision(10, 2);

            builder.Property(i => i.ApprovalNotes)
                .HasMaxLength(1000);

            builder.Property(i => i.RejectionReason)
                .HasMaxLength(1000);
        }
    }

    #endregion
}
