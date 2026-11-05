using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

public class CreateSetupIntentCommand : ICommand<Result<SetupIntentDto>>
{
}
