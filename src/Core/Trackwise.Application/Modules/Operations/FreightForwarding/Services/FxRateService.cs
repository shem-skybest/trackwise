using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

public class FxRateService(HttpClient _httpClient,
    IMemoryCache _cache,
    IOptions<FxRateOptions> _options,
    ILogger<FxRateService> _logger) : IFxRateService
{
    private const string CacheKey = "currencylayer_live_quotes";
    public async Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromCurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(toCurrency);

        var from = fromCurrency.Trim().ToUpperInvariant();
        var to = toCurrency.Trim().ToUpperInvariant();

        if (from == to)
            return 1.0m;

        var quotes = await GetLiveQuotesAsync(ct);
        if (quotes is null || quotes.Count == 0)
            return null;

        // Rate from USD to "from" currency (USD to USD is 1.0)
        decimal fromRateInUsd = 1.0m;
        if (from != "USD")
        {
            if (!quotes.TryGetValue($"USD{from}", out fromRateInUsd) || fromRateInUsd <= 0m)
            {
                _logger.LogWarning("CurrencyLayer rate for pair 'USD{From}' not found.", from);
                return null;
            }
        }

        // Rate from USD to "to" currency (USD to USD is 1.0)
        decimal toRateInUsd = 1.0m;
        if (to != "USD")
        {
            if (!quotes.TryGetValue($"USD{to}", out toRateInUsd) || toRateInUsd <= 0m)
            {
                _logger.LogWarning("CurrencyLayer rate for pair 'USD{To}' not found.", to);
                return null;
            }
        }

        // Triangulate: (USD -> TO) / (USD -> FROM)
        return Math.Round(toRateInUsd / fromRateInUsd, 6);
    }

    private async Task<IReadOnlyDictionary<string, decimal>?> GetLiveQuotesAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, decimal>? cachedQuotes))
            return cachedQuotes;

        if (string.IsNullOrWhiteSpace(_options.Value.AccessKey))
        {
            _logger.LogError("CurrencyLayer AccessKey is missing in configuration.");
            return null;
        }

        try
        {
            // Omitting the 'currencies' parameter fetches quotes for all available currencies in one call
            var requestUri = $"{_options.Value.BaseUrl.TrimEnd('/')}/live?access_key={_options.Value.AccessKey}&format=1";

            var response = await _httpClient.GetFromJsonAsync<CurrencyLayerLiveResponse>(requestUri, ct);

            if (response is null || !response.Success)
            {
                _logger.LogError("CurrencyLayer API returned an error: Code {Code} - {Info}",
                    response?.Error?.Code,
                    response?.Error?.Info ?? "Unknown error");
                return null;
            }

            var entryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(_options.Value.CacheDurationHours));

            _cache.Set(CacheKey, response.Quotes, entryOptions);

            return response.Quotes;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP failure while contacting CurrencyLayer API.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving CurrencyLayer quotes.");
            return null;
        }
    }
}
