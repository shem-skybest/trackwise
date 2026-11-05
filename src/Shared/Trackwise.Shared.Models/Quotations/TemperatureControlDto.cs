using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;

namespace Trackwise.Shared.Models.Quotations;

public record TemperatureControlDto
{
    public TemperatureZone Zone { get; init; }
    public decimal SetpointCelsius { get; init; }
    public decimal AllowableVarianceCelsius { get; init; }
    public ComplianceRegime Compliance { get; init; }
    public bool RequiresContinuousTelematics { get; init; }

    /// <summary>
    /// Parameterless constructor required for JSON deserialization (System.Text.Json / ASP.NET Core model binding).
    /// </summary>
    public TemperatureControlDto() { }

    /// <summary>
    /// Positional constructor for query mapping and testing.
    /// </summary>
    public TemperatureControlDto(
        TemperatureZone zone,
        decimal setpointCelsius,
        decimal allowableVarianceCelsius,
        ComplianceRegime compliance,
        bool requiresContinuousTelematics)
    {
        Zone = zone;
        SetpointCelsius = setpointCelsius;
        AllowableVarianceCelsius = allowableVarianceCelsius;
        Compliance = compliance;
        RequiresContinuousTelematics = requiresContinuousTelematics;
    }
}
