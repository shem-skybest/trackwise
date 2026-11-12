namespace Trackwise.Application.Abstractions.Payments.Xendit;

/// <summary>
/// Metadata keys written onto a Xendit invoice and read back on its callback.
/// </summary>
public static class XenditMetadataKeys
{
    public const string TenantId = "tenant_id";
    public const string InvoiceId = "invoice_id";
    public const string PaymentLinkId = "payment_link_id";
}
