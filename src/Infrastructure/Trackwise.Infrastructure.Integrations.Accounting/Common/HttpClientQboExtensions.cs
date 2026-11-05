using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Trackwise.Infrastructure.Integrations.Accounting.Common;

/// <summary>
///     HttpClient extensions for the QuickBooks Online Data API. Unlike the ELD read helpers,
///     these throw <see cref="QboApiException"/> on failure so a push error surfaces to the
///     sync job (which records it against the entity mapping) rather than being swallowed.
/// </summary>
internal static class HttpClientQboExtensions
{
    public static async Task<TResponse> GetQboAsync<TResponse>(
        this HttpClient client, string url, string accessToken, string action, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        Authorize(request, accessToken);
        return await SendAsync<TResponse>(client, request, action, ct);
    }

    public static async Task<TResponse> PostQboAsync<TResponse>(
        this HttpClient client, string url, object payload, string accessToken, string action, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload, options: QboJsonOptions.Default)
        };
        Authorize(request, accessToken);
        return await SendAsync<TResponse>(client, request, action, ct);
    }

    private static void Authorize(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static async Task<TResponse> SendAsync<TResponse>(
        HttpClient client, HttpRequestMessage request, string action, CancellationToken ct)
    {
        var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new QboApiException(action, response.StatusCode, body);
        }

        var result = await response.Content.ReadFromJsonAsync<TResponse>(QboJsonOptions.Default, ct);
        return result ?? throw new QboApiException(action, response.StatusCode, "empty response body");
    }
}
