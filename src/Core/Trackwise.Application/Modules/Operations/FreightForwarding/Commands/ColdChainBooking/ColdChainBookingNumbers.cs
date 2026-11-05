using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Where a new Cold Chain booking's BKN-RFV number comes from: TrackWise's own counter, or - while
/// nSFS still issues them - typed in from nSFS, now or once nSFS has it.
/// </summary>
internal static class ColdChainBookingNumbers
{
    /// <summary>
    /// The number a new booking starts with: the next one from the counter, or the normalized
    /// <paramref name="typed" /> one (null when not typed yet) while numbers are typed in.
    /// </summary>
    /// <exception cref="InvalidOperationException">A number was typed when TrackWise issues them, or it's taken.</exception>
    public static async Task<string?> ForNewBookingAsync(
        ITenantUnitOfWork tenantUow, IWorkfileReferenceGenerator referenceGenerator, string? typed, CancellationToken ct)
    {
        if (!tenantUow.GetCurrentTenant().Settings.ColdChainBookingNumbersTyped)
        {
            if (!string.IsNullOrWhiteSpace(typed))
            {
                throw new InvalidOperationException(
                    "TrackWise issues the booking numbers, so leave the number blank. To type nSFS's numbers instead, switch it in Settings > Company.");
            }

            return await referenceGenerator.NextAsync(WorkfileSeries.For(WorkfileType.Reefer, null, null), ct);
        }

        return string.IsNullOrWhiteSpace(typed) ? null : await NormalizeFreeAsync(tenantUow, typed, null, ct);
    }

    /// <summary>The typed number in nSFS's form, refused when another booking already has it.</summary>
    /// <exception cref="InvalidOperationException">The text isn't a booking number, or it's taken.</exception>
    public static async Task<string> NormalizeFreeAsync(
        ITenantUnitOfWork tenantUow, string typed, Guid? bookingId, CancellationToken ct)
    {
        var today = tenantUow.GetCurrentTenant().Settings.LocalDate(DateTime.UtcNow);
        var reference = ReeferBookingNumber.Normalize(typed, today);

        var taken = await tenantUow.Repository<Booking>().Query()
            .AnyAsync(b => b.ReferenceNumber == reference && b.Id != bookingId, ct);
        if (taken)
        {
            throw new InvalidOperationException($"Another booking already has {reference}.");
        }

        return reference;
    }
}
