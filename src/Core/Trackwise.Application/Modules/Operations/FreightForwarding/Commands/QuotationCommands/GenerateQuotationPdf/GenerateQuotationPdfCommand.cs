using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.GenerateQuotationPdf;

public sealed record FileResponse(
    byte[] Content,
    string ContentType = "application/pdf",
    string FileName = "quotation.pdf"
);

public sealed record GenerateQuotationPdfCommand(Guid QuotationId) : ICommand<Result<FileResponse>>;
