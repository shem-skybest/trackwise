using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Notes;

namespace Trackwise.Application.Modules.Platform.Notes.Queries.GetNoteById;

public record GetNoteByIdQuery(Guid Id) : IRequest<Result<NoteDto>>;
