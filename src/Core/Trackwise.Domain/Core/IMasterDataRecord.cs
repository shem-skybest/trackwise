using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Core;

/// <summary>
///     A canonical master-data record: the single source of truth for one real-world thing
///     (a port, a partner, a truck, a charge code) that several systems previously each kept their
///     own copy of.
///     <para>
///     Implementing this opts an entity into the master-data machinery - field-level change
///     history, duplicate detection, steward merge, bulk import/export and the
///     <c>updated_since</c> delta feed - so the services can work over any master entity without
///     knowing its concrete type.
///     </para>
/// </summary>
public interface IMasterDataRecord : IEntity<Guid>, IAuditableEntity, ISoftDeletable
{
    /// <summary>
    ///     Human-facing identifier, unique and stable for the life of the record. Stewards quote it,
    ///     other systems join on it, and it survives a merge - so it is never recycled.
    /// </summary>
    string Code { get; set; }

    string Name { get; set; }

    /// <summary>Business state. An inactive record still exists and still resolves by code.</summary>
    bool IsActive { get; set; }

    /// <summary>
    ///     Set when a steward merged this record into another. The row stays as a tombstone so that
    ///     historical references - and every legacy key pointing at it - keep resolving, following
    ///     the chain to the surviving record.
    /// </summary>
    Guid? MergedIntoId { get; set; }

    /// <summary>
    ///     Which entity group this record belongs to. Drives steward permissions, whether the
    ///     entity needs maker-checker approval, and how the module groups its navigation.
    ///     <para>
    ///     An instance property rather than a static abstract one, even though the value is fixed
    ///     per type: a static abstract member would make <c>IMasterDataRecord</c> illegal as a type
    ///     argument, and the change-log interceptor exists precisely to walk
    ///     <c>ChangeTracker.Entries&lt;IMasterDataRecord&gt;()</c>. It is kept out of the database by
    ///     <c>ApplyMasterDataConventions</c>.
    ///     </para>
    /// </summary>
    MasterDataGroup Group { get; }
}
