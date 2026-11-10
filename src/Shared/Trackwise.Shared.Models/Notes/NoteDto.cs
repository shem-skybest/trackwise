using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Shared.Models.Notes;

public record NoteDto(
    Guid Id,
    string? Title,
    string Content,
    int DisplayOrder,
    bool IsPinned,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy
);
