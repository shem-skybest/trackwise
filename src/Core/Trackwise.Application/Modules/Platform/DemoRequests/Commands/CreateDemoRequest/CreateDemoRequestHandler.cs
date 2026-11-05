using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Platform.DemoRequests.Commands;

internal sealed class CreateDemoRequestHandler(
    IMasterUnitOfWork masterUow,
    ILogger<CreateDemoRequestHandler> logger) : IAppRequestHandler<CreateDemoRequestCommand, Result>
{
    public async Task<Result> Handle(CreateDemoRequestCommand req, CancellationToken ct)
    {
        var demoRequest = new DemoRequest
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            Company = req.Company,
            Phone = req.Phone,
            FleetSize = req.FleetSize,
            Message = req.Message,
        };

        await masterUow.Repository<DemoRequest>().AddAsync(demoRequest, ct);
        await masterUow.SaveChangesAsync(ct);

        logger.LogInformation("Created demo request from {Email} at {Company}", req.Email, req.Company);
        return Result.Ok();
    }
}
