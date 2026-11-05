using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// An ocean file. The direction decides both the reference series and whether billing-forward has
/// to ask for a VAT treatment - exports do, everything else must not.
/// </summary>
public class RegularWorkfile : Workfile
{
    public override WorkfileType Type { get; protected set; } = WorkfileType.Regular;

    public FreightMode Direction { get; private set; }

    public override string ReferenceSeries => WorkfileSeries.ForRegular(Direction);

    public override bool PromptsForVatTreatment => Direction is FreightMode.Export;

    /// <summary>A triangular shipment never touches a Philippine port, so it has nothing to truck.</summary>
    public override bool SupportsTruckLegs => Direction is not FreightMode.Triangular;

    public static RegularWorkfile Create(
        string referenceNumber,
        FreightMode direction,
        Guid customerId,
        Guid? bookingId,
        WorkfileHeaderData header)
    {
        var workfile = new RegularWorkfile { Direction = direction };
        workfile.Initialize(referenceNumber, customerId, bookingId, header);
        workfile.DomainEvents.Add(new WorkfileCreated(workfile.Id, referenceNumber, bookingId));
        return workfile;
    }
}
