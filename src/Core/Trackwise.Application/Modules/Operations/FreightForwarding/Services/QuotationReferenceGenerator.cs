using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

internal sealed class QuotationReferenceGenerator(ITenantUnitOfWork tenantUow, TimeProvider clock)
    : IQuotationReferenceGenerator
{
    private const string Prefix = "QT";
    private const int SequenceWidth = 5;

    public async Task<string> NextAsync(FreightMode freightMode, string branchCode, CancellationToken ct)
    {
        var prefix = BuildPrefix(freightMode, branchCode);

        // Sequence is scoped to year + freight mode + branch. Order by length first so a
        // sequence that outgrows the padding (100000) still sorts after 99999.
        var last = await tenantUow.Repository<Quotation>().Query()
            .AsNoTracking()
            .Where(q => q.ReferenceNumber.StartsWith(prefix))
            .OrderByDescending(q => q.ReferenceNumber.Length)
            .ThenByDescending(q => q.ReferenceNumber)
            .Select(q => q.ReferenceNumber)
            .FirstOrDefaultAsync(ct);

        var next = TryParseSequence(last, prefix, out var current) ? current + 1 : 1;
        return Format(prefix, next);
    }

    /// <summary>e.g. "QT-26-DOMMNL-"</summary>
    private string BuildPrefix(FreightMode freightMode, string branchCode)
    {
        var code = Branch.NormalizeCode(branchCode);
        if (code.Length == 0)
            throw new ArgumentException("Branch code is required to generate a quotation reference.", nameof(branchCode));

        var year = (clock.GetUtcNow().Year % 100).ToString("D2");
        var mode = freightMode.ToString()[..3].ToUpperInvariant();

        return $"{Prefix}-{year}-{mode}{code}-";
    }

    private static bool TryParseSequence(string? reference, string prefix, out int sequence)
    {
        sequence = 0;
        return reference is not null
            && reference.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(reference.AsSpan(prefix.Length), out sequence);
    }

    private static string Format(string prefix, int sequence) =>
        $"{prefix}{sequence.ToString($"D{SequenceWidth}")}";
}
