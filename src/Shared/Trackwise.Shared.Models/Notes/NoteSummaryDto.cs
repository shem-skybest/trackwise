using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Shared.Models.Notes;

public record NoteSummaryDto(
    Guid Id,
    string? Title,
    string ContentPreview,
    int DisplayOrder,
    bool IsPinned,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
