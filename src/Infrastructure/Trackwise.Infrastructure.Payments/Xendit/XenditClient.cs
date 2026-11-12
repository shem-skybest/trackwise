using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Payments.Xendit;

namespace Trackwise.Infrastructure.Payments.Xendit;

/// <summary>
/// Thin client for Xendit's Invoice API (https://developers.xendit.co/api-reference/#invoices).
/// Auth is HTTP Basic with the secret key as the username and an empty password. The key is
/// passed per call because each tenant uses its own Xendit account.
/// </summary>
internal sealed class XenditClient(HttpClient http, ILogger<XenditClient> logger) : IXenditClient
{
    public const string BaseAddress = "https://api.xendit.co/";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<XenditInvoice> CreateInvoiceAsync(
        string secretKey, XenditInvoiceRequest request, CancellationToken ct = default)
    {
        var body = new CreateInvoiceBody
        {
            ExternalId = request.ExternalId,
            Amount = request.Amount,
            Currency = request.Currency,
            Description = request.Description,
            SuccessRedirectUrl = request.SuccessRedirectUrl,
            FailureRedirectUrl = request.FailureRedirectUrl,
            PayerEmail = request.PayerEmail,
            InvoiceDuration = request.Duration is { } d ? (long)d.TotalSeconds : null,
            Metadata = request.Metadata
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "v2/invoices");
        message.Content = JsonContent.Create(body, options: Json);
        return await SendAsync(secretKey, message, "Create Xendit invoice", ct);
    }

    public async Task<XenditInvoice> GetInvoiceAsync(
        string secretKey, string invoiceId, CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Get, $"v2/invoices/{Uri.EscapeDataString(invoiceId)}");
        return await SendAsync(secretKey, message, "Get Xendit invoice", ct);
    }

    public async Task<string?> ValidateSecretKeyAsync(string secretKey, CancellationToken ct = default)
    {
        // Listing invoices needs the same Money-in permission that creating them does, so a key
        // that passes here can take payments.
        using var message = new HttpRequestMessage(HttpMethod.Get, "v2/invoices?limit=1");
        Authorize(message, secretKey);
        try
        {
            using var response = await http.SendAsync(message, ct);
            if (response.IsSuccessStatusCode)
            {
                return null;
            }

            var error = await ReadErrorAsync(response, ct);
            return (int)response.StatusCode switch
            {
                401 => "Xendit rejected the secret key. Copy it again from Settings > API Keys.",
                403 => "This key can't create invoices. Give it Money-in Write permission in Xendit.",
                _ => $"Xendit returned {(int)response.StatusCode}: {error}"
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Could not reach Xendit to validate a key");
            return "Could not reach Xendit. Try again in a moment.";
        }
    }

    private async Task<XenditInvoice> SendAsync(
        string secretKey, HttpRequestMessage message, string action, CancellationToken ct)
    {
        Authorize(message, secretKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "{Action} failed: Xendit unreachable", action);
            throw new XenditApiException("Could not reach Xendit. Try again in a moment.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorAsync(response, ct);
                logger.LogWarning("{Action} failed: {Status} {Error}", action, (int)response.StatusCode, error);
                throw new XenditApiException(error, (int)response.StatusCode);
            }

            var dto = await response.Content.ReadFromJsonAsync<InvoiceBody>(Json, ct)
                      ?? throw new XenditApiException("Xendit returned an empty response.");
            return dto.ToModel();
        }
    }

    private static void Authorize(HttpRequestMessage message, string secretKey)
    {
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
    }

    /// <summary>Xendit errors look like <c>{"error_code": "...", "message": "..."}</c>.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var raw = await response.Content.ReadAsStringAsync(ct);
        try
        {
            var error = JsonSerializer.Deserialize<ErrorBody>(raw, Json);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
            // Not JSON - fall through to the raw body.
        }

        return string.IsNullOrWhiteSpace(raw) ? response.ReasonPhrase ?? "Unknown error" : raw;
    }

    private sealed record CreateInvoiceBody
    {
        public required string ExternalId { get; init; }
        public required decimal Amount { get; init; }
        public required string Currency { get; init; }
        public required string Description { get; init; }
        public string? SuccessRedirectUrl { get; init; }
        public string? FailureRedirectUrl { get; init; }
        public string? PayerEmail { get; init; }
        public long? InvoiceDuration { get; init; }
        public IDictionary<string, string>? Metadata { get; init; }
    }

    private sealed record InvoiceBody
    {
        public string Id { get; init; } = "";
        public string ExternalId { get; init; } = "";
        public string Status { get; init; } = "";
        public decimal Amount { get; init; }
        public decimal? PaidAmount { get; init; }
        public string Currency { get; init; } = "";
        public string InvoiceUrl { get; init; } = "";
        public string? PaymentMethod { get; init; }
        public string? PaymentChannel { get; init; }
        public DateTime? PaidAt { get; init; }

        public XenditInvoice ToModel() => new()
        {
            Id = Id,
            ExternalId = ExternalId,
            Status = Status,
            Amount = Amount,
            PaidAmount = PaidAmount,
            Currency = Currency,
            InvoiceUrl = InvoiceUrl,
            PaymentMethod = PaymentMethod,
            PaymentChannel = PaymentChannel,
            PaidAt = PaidAt
        };
    }

    private sealed record ErrorBody
    {
        public string? Message { get; init; }
    }
}
