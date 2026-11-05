using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

public enum DocumentType
{
    BillOfLading,
    ProofOfDelivery,
    Invoice,
    Receipt,
    Contract,
    InsuranceCertificate,
    Photo,
    DriverLicense,
    VehicleRegistration,
    IdentityDocument,
    PickupInspection,
    DeliveryInspection,
    Other,

    [Description("DOT Inspection")]
    DotInspection,

    TitleCertificate,
    LeaseAgreement,
    MaintenanceRecord,
    AnnualInspection,

    /// <summary>A police report, or the barangay blotter a minor road incident often gets instead.</summary>
    PoliceReport
}
