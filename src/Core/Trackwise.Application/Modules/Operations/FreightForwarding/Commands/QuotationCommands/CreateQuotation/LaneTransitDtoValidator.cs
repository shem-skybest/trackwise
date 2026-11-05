using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotation;

public class LaneTransitDtoValidator : AbstractValidator<LaneTransitDto>
{
    public LaneTransitDtoValidator()
    {
        // -------------------------------------------------------------
        // Multi-Stop Routing Mode (when Stops collection is populated)
        // -------------------------------------------------------------
        When(x => x.Stops != null && x.Stops.Count > 0, () =>
        {
            RuleFor(x => x.Stops)
                .Must(stops => stops!.Count >= 2)
                .WithMessage("A multi-stop route must have at least 2 stops (origin and destination).")
                .Must(HaveUniqueAndContiguousSequences)
                .WithMessage("Stop sequences must start at 1 and increment sequentially without gaps or duplicates.")
                .Must(NotHaveConsecutiveDuplicateLocations)
                .WithMessage("Adjacent stops cannot be at the exact same location.");

            RuleForEach(x => x.Stops)
                .SetValidator(new RouteStopDtoValidator());
        });

        // -------------------------------------------------------------
        // Point-to-Point Routing Mode (default when Stops is empty/null)
        // -------------------------------------------------------------
        When(x => x.Stops == null || x.Stops.Count == 0, () =>
        {
            RuleFor(x => x.OriginLocationId)
                .NotEmpty()
                .WithMessage("Origin location is required for point-to-point routes.");

            RuleFor(x => x.DestinationLocationId)
                .NotEmpty()
                .WithMessage("Destination location is required for point-to-point routes.");

            RuleFor(x => x.DestinationLocationId)
                .NotEqual(x => x.OriginLocationId)
                .When(x => x.OriginLocationId.HasValue && x.DestinationLocationId.HasValue)
                .WithMessage("Origin and Destination locations cannot be the same site.");
        });

        // -------------------------------------------------------------
        // Shared Lane Properties (Transit Windows & Distance)
        // -------------------------------------------------------------
        RuleFor(x => x.RequestedPickupWindowStart)
            .LessThan(x => x.RequestedDeliveryWindowEnd!.Value)
            .When(x => x.RequestedPickupWindowStart.HasValue && x.RequestedDeliveryWindowEnd.HasValue)
            .WithMessage("Pickup window start must precede delivery window end.");

        RuleFor(x => x.TotalDistanceKm)
            .GreaterThan(0)
            .When(x => x.TotalDistanceKm.HasValue)
            .WithMessage("Total distance must be greater than 0 km.");
    }

    private static bool HaveUniqueAndContiguousSequences(List<RouteStopDto>? stops)
    {
        if (stops == null || stops.Count == 0) return true;

        var sequences = stops.Select(s => s.Sequence).OrderBy(s => s).ToList();
        return sequences.SequenceEqual(Enumerable.Range(1, stops.Count));
    }

    private static bool NotHaveConsecutiveDuplicateLocations(List<RouteStopDto>? stops)
    {
        if (stops == null || stops.Count < 2) return true;

        var ordered = stops.OrderBy(s => s.Sequence).ToList();
        for (int i = 0; i < ordered.Count - 1; i++)
        {
            if (ordered[i].LocationId != Guid.Empty &&
                ordered[i].LocationId == ordered[i + 1].LocationId)
            {
                return false;
            }
        }

        return true;
    }
}
