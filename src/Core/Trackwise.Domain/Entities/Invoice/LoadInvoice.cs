using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

public class LoadInvoice : Invoice
{
    public override InvoiceType Type { get; set; } = InvoiceType.Load;
    public Guid? LoadId { get; set; }
    public virtual Load? Load { get; set; }

    public required Guid CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;
}
