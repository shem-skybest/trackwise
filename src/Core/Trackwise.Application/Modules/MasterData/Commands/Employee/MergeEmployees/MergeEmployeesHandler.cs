using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeEmployeesHandler(
    ITenantUnitOfWork tenantUow,
    IMasterUnitOfWork masterUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeEmployeesCommand, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(MergeEmployeesCommand req, CancellationToken ct)
    {
        // Employee.Id is a User.Id by convention, so merging away the loser also strips tenant
        // access the moment its record stops resolving - the same risk Delete guards against, and
        // it needs the same explicit confirmation before the transaction below does anything.
        var loserUser = await masterUow.Repository<User>().GetByIdAsync(req.LoserId, ct);
        var loserHasLiveLogin =
            loserUser is not null && loserUser.DeletionRequestedAt is null && loserUser.AnonymizedAt is null;

        if (loserHasLiveLogin && !req.ConfirmedDespiteDependents)
        {
            return Result<EmployeeDto>.Fail(
                "The employee being merged away has an active platform login. Confirm this employee "
                + "should lose tenant access, with a reason.",
                ErrorCodes.MasterDataRecordInUse);
        }

        // The whole handler is the transactional unit, not just the merge: the repoint is
        // immediate SQL while the survivor's edited fields are only staged, so a failure between
        // them would move live references onto a record that was never written. Loading inside the
        // delegate is required too - a retry replays it against a cleared change tracker.
        var mergeResult = await tenantUow.ExecuteInTransactionAsync<Result<EmployeeDto>>(
            async token =>
            {
                // Both sides are read past the query filter: merging away a record a steward already
                // deleted is a normal cleanup, and blocking it would leave the duplicate stranded.
                var records = await tenantUow.Repository<Employee>().Query()
                    .IgnoreQueryFilters()
                    .Where(e => e.Id == req.SurvivorId || e.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(e => e.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(e => e.Id == req.LoserId);

                if (survivor is null)
                {
                    return TransactionOutcome<Result<EmployeeDto>>.Discard(
                        Result<EmployeeDto>.Fail($"Could not find the surviving employee '{req.SurvivorId}'."));
                }

                if (loser is null)
                {
                    return TransactionOutcome<Result<EmployeeDto>>.Discard(
                        Result<EmployeeDto>.Fail($"Could not find the employee to merge '{req.LoserId}'."));
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup
                // could then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return TransactionOutcome<Result<EmployeeDto>>.Discard(Result<EmployeeDto>.Fail(
                        $"Employee '{loser.Code}' has already been merged. Merge the surviving record instead."));
                }

                if (survivor.MergedIntoId is not null)
                {
                    return TransactionOutcome<Result<EmployeeDto>>.Discard(Result<EmployeeDto>.Fail(
                        $"Employee '{survivor.Code}' has itself been merged away and cannot be a survivor."));
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                await merges.MergeAsync(nameof(Employee), survivor, loser, req.FieldChoices, req.Reason, token);

                return TransactionOutcome<Result<EmployeeDto>>.Keep(Result<EmployeeDto>.Ok(survivor.ToDto()));
            },
            ct);

        if (!mergeResult.IsSuccess)
        {
            return mergeResult;
        }

        // Same non-transactional master-DB step Delete takes, for the same reason: a single local
        // transaction cannot span the tenant and master databases.
        var tenant = tenantUow.GetCurrentTenant();

        if (loserUser?.Tenant is not null && loserUser.Tenant.Id == tenant.Id)
        {
            loserUser.Tenant = null;
            masterUow.Repository<User>().Update(loserUser);
            await masterUow.SaveChangesAsync(ct);
        }

        return mergeResult;
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        Employee survivor, Employee loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Employee.FirstName): survivor.FirstName = loser.FirstName; break;
                case nameof(Employee.LastName): survivor.LastName = loser.LastName; break;
                case nameof(Employee.PhoneNumber): survivor.PhoneNumber = loser.PhoneNumber; break;
                case nameof(Employee.Address): survivor.Address = loser.Address; break;
                case nameof(Employee.BranchId): survivor.BranchId = loser.BranchId; break;

                // Code is never taken from the loser: it is the handle other systems join on, and
                // the loser's own code keeps resolving through the cross-reference instead. Email
                // and Salary/SalaryType/Status are not choices either - they are current operational
                // facts about the survivor, not descriptive fields either record's history can
                // outvote.
            }
        }
    }
}
