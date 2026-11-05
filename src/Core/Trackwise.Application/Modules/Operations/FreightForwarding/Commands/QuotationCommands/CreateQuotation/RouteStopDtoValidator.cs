using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotation;

public class RouteStopDtoValidator : AbstractValidator<RouteStopDto>
{
    public RouteStopDtoValidator()
    {
        RuleFor(x => x.Sequence)
            .GreaterThan(0)
            .WithMessage("Stop sequence must be a positive integer.");

        RuleFor(x => x.LocationId)
            .NotEmpty()
            .WithMessage("Stop location ID cannot be empty.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("A valid stop type (e.g. Pickup, Dropoff) must be selected.");

        RuleFor(x => x.WindowStart)
            .LessThan(x => x.WindowEnd!.Value)
            .When(x => x.WindowStart.HasValue && x.WindowEnd.HasValue)
            .WithMessage("Stop window start must precede window end.");

        RuleFor(x => x.Instructions)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Instructions))
            .WithMessage("Stop instructions cannot exceed 500 characters.");
    }
};
