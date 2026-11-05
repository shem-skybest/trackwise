using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Legs;

/// <summary>The rules adding and editing a truck leg share.</summary>
internal abstract class TruckLegFieldsValidator<T> : AbstractValidator<T> where T : TruckLegFields
{
    protected TruckLegFieldsValidator()
    {
        RuleFor(i => i.Role).IsInEnum();
        RuleFor(i => i.From).Must(HaveOneEnd).WithMessage("Pick where the leg starts: a port or a site.");
        RuleFor(i => i.To).Must(HaveOneEnd).WithMessage("Pick where the leg ends: a port or a site.");
        RuleFor(i => i)
            .Must(i => !SameEnd(i.From, i.To))
            .WithMessage("A leg can't start and end at the same place.");
        RuleFor(i => i.ContainerSlotIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("A container is listed twice.");
        RuleFor(i => i.Notes).MaximumLength(2000);
    }

    private static bool HaveOneEnd(TruckLegEnd end) => end.TerminalId.HasValue ^ end.SiteId.HasValue;

    private static bool SameEnd(TruckLegEnd a, TruckLegEnd b) =>
        (a.TerminalId.HasValue && a.TerminalId == b.TerminalId) || (a.SiteId.HasValue && a.SiteId == b.SiteId);
}
