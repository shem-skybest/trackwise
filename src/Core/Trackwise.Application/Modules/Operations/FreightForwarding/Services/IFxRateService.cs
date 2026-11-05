using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

/// <summary>
/// Currency conversion lookups. Wire this to your existing currency/exchange-rate source;
/// the domain deliberately knows nothing about FX.
/// </summary>
public interface IFxRateService
{
    /// <summary>
    /// Multiplier that converts an amount in <paramref name="fromCurrency"/> to <paramref name="toCurrency"/>,
    /// or null when no rate is available.
    /// </summary>
    Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken ct);
}
