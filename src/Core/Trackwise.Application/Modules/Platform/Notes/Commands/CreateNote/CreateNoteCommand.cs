using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Notes.Commands.CreateNote;

public record CreateNoteCommand(
    string? Title,
    string Content,
    string? UserId = null
) : IRequest<Result<Guid>>;
