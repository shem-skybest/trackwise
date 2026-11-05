using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A general trucking file. The variant picks the reference series - BKN-TRK for a truck job,
/// BKN-TRL for a trailer job.
/// <para>
/// A file with a <see cref="ParentWorkfileId"/> is a <b>truck leg</b> of a forwarding workfile: the
/// trucking that moves that file's boxes to or from the port. It replaces the nSFS SLC-TRK job
/// tagged INTERNAL, keeps that job's own number series (SLC-TRK) and is never billed on its own -
/// the client gets one bill for the move, from the parent (FORWARDING_LEGS_PLAN.md F1, F2, F6).
/// </para>
/// </summary>
public class TruckingWorkfile : Workfile
{
    public override WorkfileType Type { get; protected set; } = WorkfileType.Trucking;

    public TruckingVariant Variant { get; private set; }

    /// <summary>The forwarding workfile this file is a truck leg of; null for a standalone trucking job.</summary>
    public Guid? ParentWorkfileId { get; private set; }

    /// <summary>Set exactly when <see cref="ParentWorkfileId"/> is.</summary>
    public TruckLegRole? LegRole { get; private set; }

    public bool IsLeg => ParentWorkfileId.HasValue;

    public override string ReferenceSeries =>
        IsLeg ? WorkfileSeries.TruckLeg : WorkfileSeries.ForTrucking(Variant);

    public override bool PromptsForVatTreatment => false;

    public static TruckingWorkfile Create(
        string referenceNumber,
        TruckingVariant variant,
        Guid customerId,
        Guid? bookingId,
        WorkfileHeaderData header)
    {
        var workfile = new TruckingWorkfile { Variant = variant };
        workfile.Initialize(referenceNumber, customerId, bookingId, header);
        workfile.DomainEvents.Add(new WorkfileCreated(workfile.Id, referenceNumber, bookingId));
        return workfile;
    }

    /// <summary>
    /// Opens a truck leg under <paramref name="parent"/>. The leg bills nobody, so it takes the
    /// parent's client and carries no booking, bill indicator or commercial terms of its own; the
    /// header passed in holds only what the trucking itself needs (trucker, commodity).
    /// </summary>
    public static TruckingWorkfile CreateLeg(
        string referenceNumber,
        Workfile parent,
        TruckLegRole role,
        WorkfileHeaderData header)
    {
        parent.EnsureCanAddTruckLeg();

        var leg = new TruckingWorkfile
        {
            Variant = TruckingVariant.Truck,
            ParentWorkfileId = parent.Id,
            LegRole = role
        };
        leg.Initialize(referenceNumber, parent.CustomerId, bookingId: null, header);
        leg.DomainEvents.Add(new WorkfileCreated(leg.Id, referenceNumber, null));
        leg.DomainEvents.Add(new TruckLegAdded(parent.Id, leg.Id, referenceNumber, role));
        return leg;
    }

    /// <summary>Changes a leg's role and subcontract trucker; its ends and boxes live on its loads.</summary>
    public void UpdateLeg(TruckLegRole role, Guid? truckerId)
    {
        if (!IsLeg)
        {
            throw new InvalidOperationException($"Workfile '{ReferenceNumber}' is not a truck leg.");
        }

        UpdateHeader(new WorkfileHeaderData { TruckerId = truckerId, Commodity = Commodity });
        LegRole = role;
    }
}
