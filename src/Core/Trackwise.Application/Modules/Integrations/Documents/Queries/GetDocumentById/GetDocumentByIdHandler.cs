using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Documents.Queries;

internal sealed class GetDocumentByIdHandler : IAppRequestHandler<GetDocumentByIdQuery, Result<DocumentDto>>
{
    private readonly ITenantUnitOfWork _tenantUow;

    public GetDocumentByIdHandler(ITenantUnitOfWork tenantUow)
    {
        _tenantUow = tenantUow;
    }

    public async Task<Result<DocumentDto>> Handle(GetDocumentByIdQuery req, CancellationToken ct)
    {
        // Query the base type; EF will materialize derived type (TPH)
        var document = await _tenantUow.Repository<Document>()
            .GetAsync(d => d.Id == req.DocumentId, ct);

        if (document is null)
        {
            return Result<DocumentDto>.Fail($"Could not find document with ID '{req.DocumentId}'");
        }

        if (document.Status == DocumentStatus.Deleted)
        {
            return Result<DocumentDto>.Fail("Document has been deleted");
        }

        var dto = document.ToDto();
        return Result<DocumentDto>.Ok(dto);
    }
}
