using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Ifta.TaxRates.Commands;

public sealed class DeleteIftaTaxRateCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
