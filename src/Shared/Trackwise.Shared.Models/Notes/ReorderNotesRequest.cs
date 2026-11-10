using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Shared.Models.Notes;

public record ReorderNotesRequest(
    List<ReorderNoteItemDto> Items
);
