using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

internal sealed class QuotationReferenceGenerator(ITenantUnitOfWork tenantUow)
    : IQuotationReferenceGenerator
{
    public async Task<string> NextAsync(string FreightMode, CancellationToken ct)
    {
        var prefix = $"QT-{DateTime.UtcNow.Year}-";

        // Order by length first so QT-2026-100000 sorts after QT-2026-99999.
        var last = await tenantUow.Repository<Quotation>().Query()
            .Where(q => q.ReferenceNumber.StartsWith(prefix))
            .OrderByDescending(q => q.ReferenceNumber.Length)
            .ThenByDescending(q => q.ReferenceNumber)
            .Select(q => q.ReferenceNumber)
            .FirstOrDefaultAsync(ct);

        var next = last is not null && int.TryParse(last.AsSpan(prefix.Length), out var n) ? n + 1 : 1;
        return $"{prefix}{FreightMode}-{next:D5}";
    }
}
