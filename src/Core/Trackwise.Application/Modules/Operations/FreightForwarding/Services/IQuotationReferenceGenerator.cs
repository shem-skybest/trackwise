using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

public interface IQuotationReferenceGenerator
{
    Task<string> NextAsync(FreightMode freightMode, string branchCode, CancellationToken ct);
}
