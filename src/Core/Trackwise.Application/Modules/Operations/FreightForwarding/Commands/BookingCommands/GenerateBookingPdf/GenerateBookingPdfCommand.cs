using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.GenerateQuotationPdf;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.GenerateBookingPdf;

/// <param name="IncludeInternalCosts">
/// Adds buying cost and margin and stamps the PDF "INTERNAL COPY". The endpoint MUST authorize this
/// flag (cost-viewing permission) before passing it through; the handler does not know the caller's rights.
/// </param>
public sealed record GenerateBookingPdfCommand(Guid BookingId, bool IncludeInternalCosts = false)
    : ICommand<Result<FileResponse>>;   // use the same base interface GenerateQuotationPdfCommand uses
