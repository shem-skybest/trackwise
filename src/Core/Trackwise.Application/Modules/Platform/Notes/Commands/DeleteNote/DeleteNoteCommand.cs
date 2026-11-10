using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Notes.Commands.DeleteNote;

public record DeleteNoteCommand(Guid Id) : IRequest<Result>;
