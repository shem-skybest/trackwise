namespace Trackwise.Shared.Models;

/// <summary>
/// What the payment gateway says after the customer returns to the public pay page.
/// </summary>
public record PublicPaymentConfirmationDto
{
    public required PublicPaymentState State { get; init; }
}

public enum PublicPaymentState
{
    /// <summary>The gateway has the money; the payment is recorded on the invoice.</summary>
    Paid,

    /// <summary>Not paid yet (e.g. an over-the-counter code not yet paid) or the gateway could not be reached.</summary>
    Pending,

    /// <summary>Nothing was paid - the customer left the payment page without paying.</summary>
    NotPaid,

    /// <summary>The company's gateway settles by webhook only (Stripe); the page can't tell.</summary>
    NotChecked
}
