using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Notes;

namespace Trackwise.Application.Modules.Platform.Notes.Queries.GetNotes;

public record GetNotesQuery(
    string? SearchTerm = null,
    bool? IsPinned = null,
    int Page = 1,
    int PageSize = 50
) : IQuery<PagedResult<NoteSummaryDto>>;
