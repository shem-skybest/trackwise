using FluentValidation;

namespace Trackwise.Application.Modules.Compliance.Incidents;

/// <summary>The rules Create and Update share.</summary>
internal abstract class IncidentFieldsValidator<T> : AbstractValidator<T> where T : IncidentFields
{
    protected IncidentFieldsValidator()
    {
        RuleFor(i => i.Kind).IsInEnum();
        RuleFor(i => i.Severity).IsInEnum();
        RuleFor(i => i.AccidentType).IsInEnum().When(i => i.AccidentType.HasValue);
        RuleFor(i => i.OccurredAt).Must(at => at.Kind != DateTimeKind.Unspecified)
            .WithMessage("The incident time must include a timezone (e.g. an ISO 8601 'Z' or offset).");
        RuleFor(i => i.Summary).NotEmpty().MaximumLength(4000)
            .WithMessage("Say what happened (up to 4,000 characters).");
        RuleFor(i => i.Prevention).MaximumLength(4000);
        RuleFor(i => i.Place).MaximumLength(500);
        RuleFor(i => i.Latitude).InclusiveBetween(-90, 90).When(i => i.Latitude.HasValue);
        RuleFor(i => i.Longitude).InclusiveBetween(-180, 180).When(i => i.Longitude.HasValue);
        RuleFor(i => i.InjuryDescription).MaximumLength(2000);
        RuleFor(i => i.VehicleDamageDescription).MaximumLength(2000);
        RuleFor(i => i.PoliceReportNumber).MaximumLength(100);
        RuleFor(i => i.PoliceStation).MaximumLength(200);
        RuleFor(i => i.BarangayBlotterNumber).MaximumLength(100);
        RuleFor(i => i.InsuranceClaimNumber).MaximumLength(100);
        RuleFor(i => i.CrewChargeNote).MaximumLength(500);
    }
}
