using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Notes;

namespace Trackwise.Application.Modules.Platform.Notes.Commands.ReOrderNote;

public record ReorderNotesCommand(List<ReorderNoteItemDto> Items, string? UserId = null) : IRequest<Result>;
