using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Shared.Models.Notes;

public record UpdateNoteRequest(
    string? Title,
    string Content
);
