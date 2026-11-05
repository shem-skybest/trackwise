using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

public interface ITariffRateResolver
{
    Task<TariffRate?> ResolveRateAsync(
        Guid? contractId,
        string chargeCode,
        FreightMode mode,
        string? originLane = null,
        string? destinationLane = null,
        CancellationToken ct = default);
}
