using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

public record GetQuotationByIdQuery(Guid Id) : IQuery<Result<QuotationDto>>;
