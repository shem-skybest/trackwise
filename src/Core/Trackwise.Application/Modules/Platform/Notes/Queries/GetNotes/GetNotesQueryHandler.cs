using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Note;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Notes;

namespace Trackwise.Application.Modules.Platform.Notes.Queries.GetNotes;

internal sealed class GetNotesHandler(
    ITenantUnitOfWork uow,
    ILogger<GetNotesHandler> logger) : IAppRequestHandler<GetNotesQuery, PagedResult<NoteSummaryDto>>
{
    public async Task<PagedResult<NoteSummaryDto>> Handle(GetNotesQuery req, CancellationToken ct)
    {
        var query = uow.Repository<Note>()
            .Query()
            .AsNoTracking();

        if (req.IsPinned.HasValue)
        {
            query = query.Where(n => n.IsPinned == req.IsPinned.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.SearchTerm))
        {
            var term = req.SearchTerm.Trim().ToLower();
            query = query.Where(n =>
                (n.Title != null && n.Title.ToLower().Contains(term)) ||
                n.Content.ToLower().Contains(term));
        }

        query = query
            .OrderByDescending(n => n.IsPinned)
            .ThenBy(n => n.DisplayOrder)
            .ThenByDescending(n => n.UpdatedAt ?? n.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(n => new NoteSummaryDto(
                n.Id,
                n.Title,
                n.Content.Length > 120 ? n.Content.Substring(0, 120) + "..." : n.Content,
                n.DisplayOrder,
                n.IsPinned,
                n.CreatedAt,
                n.UpdatedAt))
            .ToListAsync(ct);

        logger.LogInformation("Retrieved {Count} notes for page {Page} (Total: {TotalCount})", items.Count, req.Page, totalCount);

        return PagedResult<NoteSummaryDto>.Ok(items, totalCount, req.PageSize);
    }
}
