using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Abstractions.Documents;

/// <summary>
/// Renders a quarterly IFTA report as a filing-ready PDF.
/// </summary>
public interface IIftaReportPdfService
{
    byte[] GenerateIftaReportPdf(IftaReportDto report, Tenant tenant);
}
