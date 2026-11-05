using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Commands;

internal sealed class RejectDvirReportHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RejectDvirReportCommand, Result<DvirReportDto>>
{
    public async Task<Result<DvirReportDto>> Handle(RejectDvirReportCommand req, CancellationToken ct)
    {
        var report = await tenantUow.Repository<DvirReport>().GetByIdAsync(req.ReportId, ct);
        if (report is null)
        {
            return Result<DvirReportDto>.Fail("DVIR report not found.");
        }

        if (report.Status != DvirStatus.Submitted && report.Status != DvirStatus.RequiresRepair)
        {
            return Result<DvirReportDto>.Fail("Only submitted or requires_repair reports can be rejected.");
        }

        // Reject sends the report back to draft for driver resubmission
        report.Status = DvirStatus.Rejected;
        report.ReviewedById = req.RejectedById;
        report.ReviewedAt = DateTime.UtcNow;
        report.MechanicNotes = $"Rejected: {req.RejectionReason}";

        tenantUow.Repository<DvirReport>().Update(report);
        await tenantUow.SaveChangesAsync(ct);

        return Result<DvirReportDto>.Ok(report.ToDto());
    }
}
