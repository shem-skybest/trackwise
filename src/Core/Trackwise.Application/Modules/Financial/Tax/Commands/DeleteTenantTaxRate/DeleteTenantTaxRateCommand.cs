using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Tax.Commands;

public record DeleteTenantTaxRateCommand(Guid Id) : ICommand<Result>;
