using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Shared.Models.Notes;

public record ReorderNoteItemDto(
    Guid Id,
    int DisplayOrder
);
