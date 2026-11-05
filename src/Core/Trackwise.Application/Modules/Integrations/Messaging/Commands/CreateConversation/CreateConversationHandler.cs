using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Messaging;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Messaging;

namespace Trackwise.Application.Modules.Integrations.Messaging.Commands;

internal sealed class CreateConversationHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateConversationCommand, Result<ConversationDto>>
{
    public async Task<Result<ConversationDto>> Handle(CreateConversationCommand req, CancellationToken ct)
    {
        // Validate participants exist
        var employeeRepo = tenantUow.Repository<Employee>();
        foreach (var participantId in req.ParticipantIds)
        {
            var employee = await employeeRepo.GetByIdAsync(participantId, ct);
            if (employee is null)
            {
                return Result<ConversationDto>.Fail($"Employee with ID '{participantId}' not found");
            }
        }

        // Validate load exists if specified
        if (req.LoadId.HasValue)
        {
            var load = await tenantUow.Repository<Load>().GetByIdAsync(req.LoadId.Value, ct);
            if (load is null)
            {
                return Result<ConversationDto>.Fail($"Load with ID '{req.LoadId}' not found");
            }
        }

        // Create conversation
        var conversation = new Conversation
        {
            Name = req.Name,
            LoadId = req.LoadId,
            Participants = req.ParticipantIds.Select(id => new ConversationParticipant
            {
                EmployeeId = id
            }).ToList()
        };

        await tenantUow.Repository<Conversation>().AddAsync(conversation, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<ConversationDto>.Ok(conversation.ToDto());
    }
}
