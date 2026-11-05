using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class ImportMasterDataHandler(IMasterDataImporter importer)
    : IAppRequestHandler<ImportMasterDataCommand, Result<MasterDataImportDto>>
{
    public async Task<Result<MasterDataImportDto>> Handle(
        ImportMasterDataCommand req, CancellationToken ct)
    {
        var report = await importer.ImportAsync(
            new MasterDataImportRequest(
                req.EntityType, req.Content, req.DryRun, req.AcceptDuplicates, req.DuplicateReason),
            ct);

        // A file full of bad rows is a successful answer to "what is wrong with this file", not a
        // failed request: the report is the payload either way, and folding it into Result.Fail
        // would throw away the row numbers that are the only useful part.
        return Result<MasterDataImportDto>.Ok(ToDto(report));
    }

    private static MasterDataImportDto ToDto(MasterDataImportReport report) => new()
    {
        EntityType = report.EntityType,
        DryRun = report.DryRun,
        Succeeded = report.Succeeded,
        RowsRead = report.RowsRead,
        RowsAccepted = report.RowsAccepted,
        RowsApplied = report.RowsApplied,
        Issues = report.Issues
            .OrderBy(i => i.Line)
            .ThenBy(i => i.Severity is MasterDataImportSeverity.Error ? 0 : 1)
            .Select(i => new MasterDataImportIssueDto
            {
                Line = i.Line,
                Column = i.Column,
                Message = i.Message,
                IsError = i.Severity is MasterDataImportSeverity.Error
            })
            .ToList()
    };
}
