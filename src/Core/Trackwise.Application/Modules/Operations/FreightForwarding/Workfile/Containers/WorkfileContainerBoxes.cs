using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Containers;

/// <summary>
/// Turns the fields a user typed into a slot spec: checks the size, and finds the typed box in the
/// container register - or adds it there, so the register fills itself from the files that use it.
/// </summary>
internal static class WorkfileContainerBoxes
{
    public static async Task<Result<WorkfileContainerSpec>> ResolveAsync(
        ITenantUnitOfWork tenantUow, WorkfileContainerFields fields, CancellationToken ct)
    {
        var type = await tenantUow.Repository<ContainerType>().GetByIdAsync(fields.ContainerTypeId, ct);
        if (type is null || type.MergedIntoId is not null || !type.IsActive)
        {
            return Result<WorkfileContainerSpec>.Fail("That container size is not a current record.");
        }

        Guid? boxId = null;
        var number = Container.NormalizeNumber(fields.ContainerNumber);
        if (number.Length > 0)
        {
            var box = await tenantUow.Repository<Container>().GetAsync(c => c.Number == number, ct);
            if (box is null)
            {
                box = new Container { Number = number, ContainerTypeId = type.Id };
                await tenantUow.Repository<Container>().AddAsync(box, ct);
            }
            else if (box.ContainerTypeId != type.Id)
            {
                var registered = await tenantUow.Repository<ContainerType>().GetByIdAsync(box.ContainerTypeId, ct);
                return Result<WorkfileContainerSpec>.Fail(
                    $"{number} is registered as {registered?.Code ?? "a different size"}, not {type.Code}. " +
                    "Pick that size, or correct the box under Containers.");
            }

            boxId = box.Id;
        }

        return Result<WorkfileContainerSpec>.Ok(new WorkfileContainerSpec(
            type.Id, boxId, fields.SealNumber, fields.GrossWeightKg, fields.PackageCount,
            fields.PackagingType, fields.Cbm, fields.Notes));
    }
}
