using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

public interface IQuotationReferenceGenerator
{
    Task<string> NextAsync(string FreightMode, CancellationToken ct);
}
