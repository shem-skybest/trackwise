using System.Linq.Expressions;
using Trackwise.Domain.Core;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Infrastructure.Persistence.Extensions;

/// <summary>
///     Extension methods for applying audit and soft-delete conventions to the model.
/// </summary>
internal static class ModelBuilderAuditExtensions
{
    extension(ModelBuilder modelBuilder)
    {
        /// <summary>
        ///     Apply audit-field conventions to all entities implementing <see cref="IAuditableEntity" />.
        ///     Call from <c>OnModelCreating</c> after your entity configurations.
        /// </summary>
        public void ApplyAuditableConventions()
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
                {
                    continue;
                }

                var entity = modelBuilder.Entity(entityType.ClrType);

                // Column names are left to the snake_case naming convention
                // (created_at, updated_at, created_by, updated_by).
                entity.Property(nameof(AuditableEntity.CreatedAt))
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(nameof(AuditableEntity.CreatedBy))
                    .HasMaxLength(50);

                entity.Property(nameof(AuditableEntity.UpdatedBy))
                    .HasMaxLength(50);
            }
        }

        /// <summary>
        ///     Add a global query filter hiding soft-deleted rows from every entity implementing
        ///     <see cref="ISoftDeletable" />, so ordinary queries need no <c>Where</c> clause of
        ///     their own and cannot forget one. Call from <c>OnModelCreating</c> after your entity
        ///     configurations.
        ///     <para>
        ///     A filter changes the results of every existing query against the entity, so adding
        ///     <see cref="ISoftDeletable" /> to an entity that already has callers means auditing
        ///     those callers in the same change - and reaching for <c>IgnoreQueryFilters()</c>
        ///     where a steward genuinely needs to see deleted records.
        ///     </para>
        /// </summary>
        public void ApplySoftDeleteConventions()
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Owned types and inheritance children take their parent's filter; EF rejects one
                // declared on them directly.
                if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)
                    || entityType.BaseType is not null
                    || entityType.IsOwned())
                {
                    continue;
                }

                var entity = modelBuilder.Entity(entityType.ClrType);

                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var isLive = Expression.Equal(
                    Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt)),
                    Expression.Constant(null, typeof(DateTime?)));

                entity.HasQueryFilter(Expression.Lambda(isLive, parameter));

                entity.Property(nameof(ISoftDeletable.DeletedBy))
                    .HasMaxLength(50);

                // Every query carries "deleted_at IS NULL", so the column is worth indexing.
                entity.HasIndex(nameof(ISoftDeletable.DeletedAt));
            }
        }

        /// <summary>
        ///     Keep <see cref="IMasterDataRecord.Group" /> out of the database. It is a fixed
        ///     classification of the type, not a value of the row, and is an instance property only
        ///     because a static abstract one would bar the interface from being used as a type
        ///     argument. Ignoring it centrally rather than per entity means a new master entity
        ///     cannot accidentally grow a redundant column.
        /// </summary>
        public void ApplyMasterDataConventions()
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!typeof(IMasterDataRecord).IsAssignableFrom(entityType.ClrType)
                    || entityType.BaseType is not null
                    || entityType.IsOwned())
                {
                    continue;
                }

                modelBuilder.Entity(entityType.ClrType)
                    .Ignore(nameof(IMasterDataRecord.Group));
            }
        }
    }
}
