namespace Trackwise.Shared.Models;

/// <summary>Fuel added on top when diesel is at or above <see cref="DieselPrice"/> a litre.</summary>
public record RateFuelSurchargeDto
{
    public decimal DieselPrice { get; set; }
    public decimal Amount { get; set; }
}
