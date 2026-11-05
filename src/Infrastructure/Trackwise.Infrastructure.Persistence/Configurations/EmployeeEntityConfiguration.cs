using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeEntityConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.Property(e => e.Code)
            .HasMaxLength(30)
            .IsRequired();

        // Filtered rather than plain unique, now that an employee can be soft-deleted: a deleted
        // record keeps its code, and an unfiltered index would let it hold that code hostage forever.
        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.JobTitle).HasMaxLength(100);

        builder.Property(e => e.IsActive).IsRequired();
        builder.HasIndex(e => e.IsActive);

        builder.ComplexProperty(i => i.Salary, money =>
        {
            money.Property(m => m.Amount).HasPrecision(18, 2);
            money.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.HasOne(i => i.Role)
            .WithMany(i => i.Employees)
            .HasForeignKey(i => i.RoleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Restrict, not cascade: retiring a branch must go through the dependency probe, which
        // tells the steward what is scoped to it. A cascade would silently take the employees with it.
        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.BranchId);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(e => e.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves the delta feed other systems pull with ?updated_since=.
        builder.HasIndex(e => e.UpdatedAt);
    }
}
