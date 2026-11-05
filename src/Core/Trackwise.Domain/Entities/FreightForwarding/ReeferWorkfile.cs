using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A refrigerated trucking file worked as a drop schedule. It is not booked against a carrier and
/// carries no bill of lading, so the booking number, the MBL/HBL indicator and the duplicate-BL
/// check all fall away - the three rules most likely to be applied to it by mistake.
/// </summary>
public class ReeferWorkfile : Workfile
{
    public override WorkfileType Type { get; protected set; } = WorkfileType.Reefer;

    public override string ReferenceSeries => WorkfileSeries.For(WorkfileType.Reefer, null, null);

    public override bool PromptsForVatTreatment => false;

    public static ReeferWorkfile Create(
        string referenceNumber,
        Guid customerId,
        Guid? bookingId,
        WorkfileHeaderData header)
    {
        var workfile = new ReeferWorkfile();
        workfile.Initialize(referenceNumber, customerId, bookingId, header);
        workfile.DomainEvents.Add(new WorkfileCreated(workfile.Id, referenceNumber, bookingId));
        return workfile;
    }
}
