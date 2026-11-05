using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

public sealed class FxApiResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = default!;

    [JsonPropertyName("base_code")]
    public string BaseCode { get; set; } = default!;

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
