namespace Trackwise.Application.Modules.Financial.FuelLog;

internal sealed record FuelReading(Guid Id, Guid TruckId, DateTime FilledAt, long Number, decimal Liters, int? Odometer);

/// <summary>The distance between two odometer readings and the fuel bought over it.</summary>
internal sealed record FuelLeg(int Km, decimal Liters)
{
    public decimal? KmPerLiter => Liters > 0 ? Math.Round(Km / Liters, 2) : null;
}

internal static class FuelEconomy
{
    /// <summary>
    /// Full-to-full: what was bought between two odometer readings is what the truck burned over
    /// that distance. A fill-up with a reading closes a leg holding every liter put in since the
    /// previous reading, its own included. Fill-ups without a reading, or at the same reading (a
    /// top-up before the truck moved), only add liters to the leg.
    /// </summary>
    public static Dictionary<Guid, FuelLeg> Legs(IEnumerable<FuelReading> fills)
    {
        var legs = new Dictionary<Guid, FuelLeg>();

        foreach (var truck in fills.GroupBy(f => f.TruckId))
        {
            int? lastReading = null;
            var liters = 0m;

            foreach (var fill in truck.OrderBy(f => f.FilledAt).ThenBy(f => f.Number))
            {
                liters += fill.Liters;
                if (fill.Odometer is not { } reading || reading == lastReading)
                {
                    continue;
                }

                if (lastReading is { } previous && reading >= previous)
                {
                    legs[fill.Id] = new FuelLeg(reading - previous, liters);
                }

                lastReading = reading;
                liters = 0;
            }
        }

        return legs;
    }
}
