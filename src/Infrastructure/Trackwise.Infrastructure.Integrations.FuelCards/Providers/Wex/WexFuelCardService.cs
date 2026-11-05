using Trackwise.Application.Abstractions.FuelCards;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Integrations.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Trackwise.Infrastructure.Integrations.FuelCards.Providers.Wex;

/// <summary>
/// WEX fleet card API client (WEX OTR / ClearView). Requires a partner API key.
/// </summary>
internal sealed class WexFuelCardService(
    HttpClient httpClient,
    IOptions<FuelCardsOptions> options,
    ILogger<WexFuelCardService> logger)
    : BearerFuelCardService<WexTransactionsResponse>(httpClient, logger)
{
    private readonly WexOptions options = options.Value.Wex ?? new WexOptions();

    public override FuelCardProviderType ProviderType => FuelCardProviderType.Wex;

    protected override string BaseUrl => options.BaseUrl;

    protected override string ProbeUrl => "/fleet/v1/transactions?limit=1";

    public override async Task<IReadOnlyList<FuelCardTransactionData>> GetTransactionsAsync(
        DateTime sinceUtc, CancellationToken ct = default)
    {
        var url = $"/fleet/v1/transactions?startDate={sinceUtc:yyyy-MM-dd}&endDate={DateTime.UtcNow:yyyy-MM-dd}";
        var response = await HttpClient.TryGetFromJsonAsync<WexTransactionsResponse>(
            url, Logger, "get transactions", ct: ct);

        if (response?.Transactions is null)
        {
            return [];
        }

        return response.Transactions
            .Select(WexMapper.ToTransactionData)
            .Where(t => t is not null)
            .Select(t => t!)
            .ToList();
    }
}
