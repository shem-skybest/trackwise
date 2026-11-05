using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

public sealed class CurrencyLayerLiveResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "USD";

    [JsonPropertyName("quotes")]
    public Dictionary<string, decimal> Quotes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("error")]
    public CurrencyLayerError? Error { get; set; }
}

public sealed class CurrencyLayerError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("info")]
    public string? Info { get; set; }
}
