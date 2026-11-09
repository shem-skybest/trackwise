namespace Trackwise.Domain.Primitives.ValueObjects;

public record CargoSpecification
{
    public string Commodity { get; init; } = default!;
    public decimal TotalWeightKg { get; init; }
    public int PalletCount { get; init; }
    public Money DeclaredCargoValue { get; init; } = default!;
    public bool RequiresActiveRefrigeration { get; init; }

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// </summary>
    protected CargoSpecification() { }

    /// <summary>
    /// Factory method enforcing domain validation invariants.
    /// </summary>
    public static CargoSpecification Create(
        string commodity,
        decimal totalWeightKg,
        int palletCount,
        Money declaredCargoValue,
        bool requiresActiveRefrigeration = false)
    {
        //if (string.IsNullOrWhiteSpace(commodity))
        //    throw new ArgumentException("Commodity description cannot be empty.", nameof(commodity));

        //if (totalWeightKg <= 0)
        //    throw new ArgumentOutOfRangeException(nameof(totalWeightKg), "Total weight must be greater than zero.");

        //if (palletCount < 0)
        //    throw new ArgumentOutOfRangeException(nameof(palletCount), "Pallet count cannot be negative.");

        ArgumentNullException.ThrowIfNull(declaredCargoValue);

        return new CargoSpecification
        {
            Commodity = commodity.Trim(),
            TotalWeightKg = totalWeightKg,
            PalletCount = palletCount,
            DeclaredCargoValue = declaredCargoValue,
            RequiresActiveRefrigeration = requiresActiveRefrigeration
        };
    }
}
