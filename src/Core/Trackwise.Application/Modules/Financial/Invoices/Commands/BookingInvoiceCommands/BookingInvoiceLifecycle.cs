using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Financial.Invoices.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands.BookingInvoiceCommands;

// ── Commands ─────────────────────────────────────────────────────────────────

public interface IBookingInvoiceCommand : ICommand<Result<InvoiceDto>>
{
    Guid Id { get; }
}

public sealed record SubmitBookingInvoiceCommand(Guid Id) : IBookingInvoiceCommand;
public sealed record ApproveBookingInvoiceCommand(Guid Id) : IBookingInvoiceCommand;
public sealed record RejectBookingInvoiceCommand(Guid Id, string Reason) : IBookingInvoiceCommand;
public sealed record ReopenBookingInvoiceCommand(Guid Id) : IBookingInvoiceCommand;
public sealed record IssueBookingInvoiceCommand(Guid Id) : IBookingInvoiceCommand;
public sealed record SendBookingInvoiceCommand(Guid Id, string Email) : IBookingInvoiceCommand;
public sealed record CancelBookingInvoiceCommand(Guid Id, string Reason) : IBookingInvoiceCommand;

// ── Transition table ─────────────────────────────────────────────────────────
internal static class BookingInvoiceLifecycle
{
    private static readonly Dictionary<InvoiceStatus, InvoiceStatus[]> Allowed = new()
    {
        [InvoiceStatus.Draft] = [InvoiceStatus.PendingApproval, InvoiceStatus.Cancelled],
        [InvoiceStatus.PendingApproval] = [InvoiceStatus.Approved, InvoiceStatus.Rejected, InvoiceStatus.Cancelled],
        [InvoiceStatus.Rejected] = [InvoiceStatus.Draft, InvoiceStatus.Cancelled],
        [InvoiceStatus.Approved] = [InvoiceStatus.Issued],
        [InvoiceStatus.Issued] = [InvoiceStatus.Sent, InvoiceStatus.PartiallyPaid, InvoiceStatus.Paid, InvoiceStatus.Overdue],
        [InvoiceStatus.Sent] = [InvoiceStatus.Sent, InvoiceStatus.PartiallyPaid, InvoiceStatus.Paid, InvoiceStatus.Overdue],
        [InvoiceStatus.PartiallyPaid] = [InvoiceStatus.Paid, InvoiceStatus.Overdue],
        [InvoiceStatus.Overdue] = [InvoiceStatus.PartiallyPaid, InvoiceStatus.Paid],
        [InvoiceStatus.Paid] = [],
        [InvoiceStatus.Cancelled] = [],
    };

    public static bool CanMove(InvoiceStatus from, InvoiceStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}

// ── Shared handler base ──────────────────────────────────────────────────────

internal abstract class BookingInvoiceTransitionHandler<TCommand>(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<TCommand, Result<InvoiceDto>>
    where TCommand : IBookingInvoiceCommand
{
    protected abstract InvoiceStatus Target { get; }

    /// <summary>Return an error message to block the transition, or null to allow it.</summary>
    protected virtual string? Validate(BookingInvoice invoice, TCommand command) => null;

    /// <summary>Side effects beyond the status change.</summary>
    protected virtual void Apply(BookingInvoice invoice, TCommand command) { }

    protected virtual Task ApplyAsync(BookingInvoice invoice, TCommand command, CancellationToken ct)
    {
        Apply(invoice, command);
        return Task.CompletedTask;
    }

    protected virtual Task<string?> ValidateAsync(BookingInvoice invoice, TCommand command, CancellationToken ct) =>
        Task.FromResult(Validate(invoice, command));

    public async Task<Result<InvoiceDto>> Handle(TCommand command, CancellationToken ct)
    {
        var invoice = await tenantUow.Repository<BookingInvoice>().Query()
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == command.Id, ct);

        if (invoice is null)
            return Result<InvoiceDto>.Fail($"Could not find a booking invoice with ID '{command.Id}'");

        if (await ValidateAsync(invoice, command, ct) is { } error)
            return Result<InvoiceDto>.Fail(error);

        if (!BookingInvoiceLifecycle.CanMove(invoice.Status, Target))
            return Result<InvoiceDto>.Fail($"A {invoice.Status} invoice cannot move to {Target}.");

        await ApplyAsync(invoice, command, ct);
        invoice.Status = Target;

        tenantUow.Repository<BookingInvoice>().Update(invoice);
        await tenantUow.SaveChangesAsync(ct);

        return Result<InvoiceDto>.Ok(invoice.ToDto());
    }
}

// ── Handlers ─────────────────────────────────────────────────────────────────

internal sealed class SubmitBookingInvoiceHandler(ITenantUnitOfWork uow)
    : BookingInvoiceTransitionHandler<SubmitBookingInvoiceCommand>(uow)
{
    protected override InvoiceStatus Target => InvoiceStatus.PendingApproval;

    protected override string? Validate(BookingInvoice invoice, SubmitBookingInvoiceCommand _) =>
        invoice.LineItems.Count == 0 ? "Add at least one line item before submitting."
        : invoice.Total.Amount <= 0 ? "The invoice total must be greater than zero."
        : null;
}

internal sealed class ApproveBookingInvoiceHandler(ITenantUnitOfWork uow)
    : BookingInvoiceTransitionHandler<ApproveBookingInvoiceCommand>(uow)
{
    protected override InvoiceStatus Target => InvoiceStatus.Approved;
}

internal sealed class RejectBookingInvoiceHandler(ITenantUnitOfWork uow)
    : BookingInvoiceTransitionHandler<RejectBookingInvoiceCommand>(uow)
{
    protected override InvoiceStatus Target => InvoiceStatus.Rejected;

    protected override string? Validate(BookingInvoice invoice, RejectBookingInvoiceCommand cmd) =>
        string.IsNullOrWhiteSpace(cmd.Reason) ? "A rejection reason is required." : null;

    protected override void Apply(BookingInvoice invoice, RejectBookingInvoiceCommand cmd) =>
        invoice.Notes = AppendNote(invoice.Notes, $"Rejected: {cmd.Reason.Trim()}");

    internal static string AppendNote(string? existing, string note) =>
        string.IsNullOrWhiteSpace(existing) ? note : $"{existing}\n{note}";
}

internal sealed class ReopenBookingInvoiceHandler : BookingInvoiceTransitionHandler<ReopenBookingInvoiceCommand>
{
    private readonly ITenantUnitOfWork tenantUow;

    public ReopenBookingInvoiceHandler(ITenantUnitOfWork tenantUow) : base(tenantUow)
    {
        this.tenantUow = tenantUow;
    }

    // Back to Draft so CreateDraftInvoiceOnBookingConfirmed.Reconcile can edit the lines again.
    protected override InvoiceStatus Target => InvoiceStatus.Draft;

    protected override async Task<string?> ValidateAsync(
        BookingInvoice invoice,
        ReopenBookingInvoiceCommand command,
        CancellationToken ct)
    {
        var existingDraft = await tenantUow.Repository<BookingInvoice>().Query()
            .AnyAsync(i => i.BookingId == invoice.BookingId &&
                i.Id != invoice.Id &&
                i.Status == InvoiceStatus.Draft,
                ct);

        return existingDraft
            ? "Resolve the existing draft invoice before reopening this invoice."
            : null;
    }

    protected override async Task ApplyAsync(
        BookingInvoice invoice,
        ReopenBookingInvoiceCommand command,
        CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().Query()
            .Include(b => b.Charges)
            .FirstOrDefaultAsync(b => b.Id == invoice.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking '{invoice.BookingId}' for invoice '{invoice.Id}' was not found.");

        invoice.Status = InvoiceStatus.Draft;
        await BookingInvoiceDraftBuilder.CreateOrReconcileAsync(
            tenantUow,
            booking,
            invoice,
            invoice.Purpose,
            invoice.RelatedInvoiceId,
            ct);
    }
}

internal sealed class IssueBookingInvoiceHandler(ITenantUnitOfWork uow)
    : BookingInvoiceTransitionHandler<IssueBookingInvoiceCommand>(uow)
{
    protected override InvoiceStatus Target => InvoiceStatus.Issued;

    // Payment terms run from the issue date, not from when the draft was generated.
    protected override void Apply(BookingInvoice invoice, IssueBookingInvoiceCommand _) =>
        invoice.DueDate = DateTime.UtcNow.AddDays(invoice.Booking.PaymentTermsDays);
}

internal sealed class SendBookingInvoiceHandler(ITenantUnitOfWork uow)
    : BookingInvoiceTransitionHandler<SendBookingInvoiceCommand>(uow)
{
    protected override InvoiceStatus Target => InvoiceStatus.Sent;

    protected override string? Validate(BookingInvoice invoice, SendBookingInvoiceCommand cmd) =>
        string.IsNullOrWhiteSpace(cmd.Email) ? "A recipient email is required." : null;

    protected override void Apply(BookingInvoice invoice, SendBookingInvoiceCommand cmd)
    {
        invoice.SentAt = DateTime.UtcNow;
        invoice.SentToEmail = cmd.Email.Trim();
    }
}

internal sealed class CancelBookingInvoiceHandler : BookingInvoiceTransitionHandler<CancelBookingInvoiceCommand>
{
    private readonly ITenantUnitOfWork tenantUow;

    public CancelBookingInvoiceHandler(ITenantUnitOfWork tenantUow) : base(tenantUow)
    {
        this.tenantUow = tenantUow;
    }

    protected override InvoiceStatus Target => InvoiceStatus.Cancelled;

    protected override string? Validate(BookingInvoice invoice, CancelBookingInvoiceCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Reason))
            return "A cancellation reason is required.";
        if (invoice.Payments.Count > 0)
            return "An invoice with payments cannot be cancelled. Use a credit note and refund workflow.";
        if (invoice.Status is not (InvoiceStatus.Draft or InvoiceStatus.PendingApproval or InvoiceStatus.Rejected))
            return "An approved or issued invoice cannot be cancelled. Use a credit note so the accounting trail remains intact.";

        return null;
    }

    protected override async Task ApplyAsync(
        BookingInvoice invoice,
        CancelBookingInvoiceCommand cmd,
        CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().Query()
            .Include(b => b.Charges)
            .FirstOrDefaultAsync(b => b.Id == invoice.BookingId, ct)
            ?? throw new InvalidOperationException($"Booking '{invoice.BookingId}' for invoice '{invoice.Id}' was not found.");

        booking.ReleaseChargesFromInvoice(invoice.Id);
        invoice.CancelledAt = DateTime.UtcNow;
        invoice.CancellationReason = cmd.Reason.Trim();
        invoice.Notes = RejectBookingInvoiceHandler.AppendNote(
            invoice.Notes,
            $"Cancelled: {invoice.CancellationReason}");
    }
}
