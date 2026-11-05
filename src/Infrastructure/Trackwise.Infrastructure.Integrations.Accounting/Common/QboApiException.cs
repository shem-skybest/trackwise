namespace Trackwise.Infrastructure.Integrations.Accounting.Common;

/// <summary>
///     Raised when a QuickBooks Online API call returns a non-success status. Carries the raw
///     response body so the sync job can record it against the failed entity mapping.
/// </summary>
internal sealed class QboApiException(string action, System.Net.HttpStatusCode statusCode, string? body)
    : Exception($"QBO {action} failed with {(int)statusCode} {statusCode}: {Truncate(body)}")
{
    public System.Net.HttpStatusCode StatusCode { get; } = statusCode;

    private static string Truncate(string? body) =>
        string.IsNullOrEmpty(body) ? "(no body)" :
        body.Length <= 1000 ? body : body[..1000];
}
