using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class RecordTripRescueValidator : AbstractValidator<RecordTripRescueCommand>
{
    public RecordTripRescueValidator()
    {
        RuleFor(i => i.TruckId).NotEmpty().WithMessage("Pick the rescue truck.");
        RuleFor(i => i.DriverId).NotEmpty().WithMessage("Pick the rescue truck's driver.");
        RuleFor(i => i.StopIds).NotEmpty().WithMessage("Pick the drops the rescue truck took over.");
        RuleFor(i => i.Reason).NotEmpty().MaximumLength(500)
            .WithMessage("Say why the trip needed a rescue (up to 500 characters).");
        RuleFor(i => i.Place).MaximumLength(500);
        RuleFor(i => i.TransferredAt).Must(at => at.Kind != DateTimeKind.Unspecified)
            .WithMessage("Transfer time must include a timezone (e.g. an ISO 8601 'Z' or offset).");
    }
}
