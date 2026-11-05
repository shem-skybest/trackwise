using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;

namespace Trackwise.Domain.Primitives.ValueObjects;

public record TemperatureControl
{
    public TemperatureZone Zone { get; init; }
    public decimal SetpointCelsius { get; init; }
    public decimal AllowableVarianceCelsius { get; init; }
    public ComplianceRegime Compliance { get; init; }
    public bool RequiresContinuousTelematics { get; init; }

    public static TemperatureControl Create(
        TemperatureZone zone,
        decimal setpoint,
        decimal variance,
        ComplianceRegime compliance,
        bool requiresTelematics)
    {
        if (zone == TemperatureZone.Chilled && (setpoint < 2.0m || setpoint > 8.0m))
            throw new ArgumentOutOfRangeException(nameof(setpoint), "Chilled setpoint must be between 2°C and 8°C.");

        return new TemperatureControl
        {
            Zone = zone,
            SetpointCelsius = setpoint,
            AllowableVarianceCelsius = variance,
            Compliance = compliance,
            RequiresContinuousTelematics = requiresTelematics
        };
    }
}
