namespace Trackwise.Shared.Models;

public record DieselPriceDto
{
    public Guid Id { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public decimal PricePerLitre { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
