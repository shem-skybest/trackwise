using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Ifta.TaxRates.Commands;

public sealed class UpdateIftaTaxRateCommand : ICommand<Result>, IIftaTaxRateFields
{
    public Guid Id { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string? Region { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public decimal RatePerGallon { get; set; }
    public decimal? SurchargeRatePerGallon { get; set; }
}
