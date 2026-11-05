using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Documents;
using Trackwise.Application.Modules.Compliance.Ifta.Services;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Reports.Queries;

internal sealed class ExportIftaReportPdfHandler(
    IIftaReportService iftaReportService,
    IIftaReportPdfService pdfService,
    ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ExportIftaReportPdfQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(ExportIftaReportPdfQuery req, CancellationToken ct)
    {
        var report = await iftaReportService.GetReportAsync(req.Year, req.Quarter, ct);
        var pdf = pdfService.GenerateIftaReportPdf(report, tenantUow.GetCurrentTenant());
        return Result<byte[]>.Ok(pdf);
    }
}
