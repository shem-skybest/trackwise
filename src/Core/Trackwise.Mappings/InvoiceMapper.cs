using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class InvoiceMapper
{
    /// <summary>
    /// Overrides the payroll invoice's embedded employee with one resolved past the soft-delete
    /// filter. Lazy loading is on, and <c>Employee</c> is soft-deletable, so
    /// <c>payrollInvoice.Employee</c> would NRE for a retired employee - the base overload only
    /// reads it null-safely, which blanks the field rather than showing who the invoice was for.
    /// Ignored for every other invoice type.
    /// </summary>
    public static InvoiceDto ToDto(this Invoice entity, EmployeeDto? payrollEmployee)
    {
        var dto = entity.ToDto();
        return entity is PayrollInvoice ? dto with { Employee = payrollEmployee } : dto;
    }

    public static InvoiceDto ToDto(this Invoice entity)
    {
        return entity switch
        {
            BookingInvoice bookingInvoice => new InvoiceDto
            {
                Id = bookingInvoice.Id,
                Type = InvoiceType.Booking,
                Number = bookingInvoice.Number,
                Status = bookingInvoice.Status,
                CreatedDate = bookingInvoice.CreatedAt,
                Subtotal = bookingInvoice.Subtotal,
                TaxTotal = bookingInvoice.TaxTotal,
                Total = bookingInvoice.Total,
                TaxBehavior = bookingInvoice.TaxBehavior,
                TaxBreakdown = bookingInvoice.GetTaxBreakdown().ToDto(),
                DueDate = bookingInvoice.DueDate,
                Notes = bookingInvoice.Notes,
                Payments = bookingInvoice.Payments.Select(i => i.ToDto()),
                LineItems = bookingInvoice.LineItems.Select(i => i.ToDto()),
                SentAt = bookingInvoice.SentAt,
                SentToEmail = bookingInvoice.SentToEmail,
                BookingId = bookingInvoice.BookingId,
                BookingReference = bookingInvoice.Booking?.BookingNumber ?? string.Empty,
                BookingInvoicePurpose = bookingInvoice.Purpose,
                RelatedInvoiceId = bookingInvoice.RelatedInvoiceId,
                CancelledAt = bookingInvoice.CancelledAt,
                CancellationReason = bookingInvoice.CancellationReason,
                CustomerId = bookingInvoice.CustomerId,
                Customer = bookingInvoice.Customer.ToDto()
            },
            LoadInvoice loadInvoice => new InvoiceDto
            {
                Id = loadInvoice.Id,
                Type = InvoiceType.Load,
                Number = loadInvoice.Number,
                Status = loadInvoice.Status,
                CreatedDate = loadInvoice.CreatedAt,
                Subtotal = loadInvoice.Subtotal,
                TaxTotal = loadInvoice.TaxTotal,
                Total = loadInvoice.Total,
                TaxBehavior = loadInvoice.TaxBehavior,
                TaxBreakdown = loadInvoice.GetTaxBreakdown().ToDto(),
                DueDate = loadInvoice.DueDate,
                Notes = loadInvoice.Notes,
                StripeInvoiceId = loadInvoice.StripeInvoiceId,
                Payments = loadInvoice.Payments.Select(i => i.ToDto()),
                LineItems = loadInvoice.LineItems.Select(i => i.ToDto()),
                SentAt = loadInvoice.SentAt,
                SentToEmail = loadInvoice.SentToEmail,
                LoadId = loadInvoice.LoadId,
                LoadNumber = loadInvoice.Load?.Number ?? 0,
                CustomerId = loadInvoice.CustomerId,
                Customer = loadInvoice.Customer.ToDto()
            },
            SubscriptionInvoice subscriptionInvoice => new InvoiceDto
            {
                Id = subscriptionInvoice.Id,
                Type = InvoiceType.Subscription,
                Number = subscriptionInvoice.Number,
                Status = subscriptionInvoice.Status,
                CreatedDate = subscriptionInvoice.CreatedAt,
                Subtotal = subscriptionInvoice.Subtotal,
                TaxTotal = subscriptionInvoice.TaxTotal,
                Total = subscriptionInvoice.Total,
                TaxBehavior = subscriptionInvoice.TaxBehavior,
                TaxBreakdown = subscriptionInvoice.GetTaxBreakdown().ToDto(),
                DueDate = subscriptionInvoice.DueDate,
                Notes = subscriptionInvoice.Notes,
                StripeInvoiceId = subscriptionInvoice.StripeInvoiceId,
                Payments = subscriptionInvoice.Payments.Select(i => i.ToDto()),
                LineItems = subscriptionInvoice.LineItems.Select(i => i.ToDto()),
                SentAt = subscriptionInvoice.SentAt,
                SentToEmail = subscriptionInvoice.SentToEmail,
                SubscriptionId = subscriptionInvoice.SubscriptionId,
                BillingPeriodStart = subscriptionInvoice.BillingPeriodStart,
                BillingPeriodEnd = subscriptionInvoice.BillingPeriodEnd
            },
            PayrollInvoice payrollInvoice => new InvoiceDto
            {
                Id = payrollInvoice.Id,
                Type = InvoiceType.Payroll,
                Number = payrollInvoice.Number,
                Status = payrollInvoice.Status,
                CreatedDate = payrollInvoice.CreatedAt,
                Subtotal = payrollInvoice.Subtotal,
                TaxTotal = payrollInvoice.TaxTotal,
                Total = payrollInvoice.Total,
                TaxBehavior = payrollInvoice.TaxBehavior,
                TaxBreakdown = payrollInvoice.GetTaxBreakdown().ToDto(),
                DueDate = payrollInvoice.DueDate,
                Notes = payrollInvoice.Notes,
                StripeInvoiceId = payrollInvoice.StripeInvoiceId,
                Payments = payrollInvoice.Payments.Select(i => i.ToDto()),
                LineItems = payrollInvoice.LineItems.Select(i => i.ToDto()),
                SentAt = payrollInvoice.SentAt,
                SentToEmail = payrollInvoice.SentToEmail,
                EmployeeId = payrollInvoice.EmployeeId,

                // Null-safe: Employee is soft-deletable, and this lazy-loaded navigation returns
                // null for a retired employee under the global filter. Callers that need the name
                // to survive retirement pass an already-resolved value to the overload below instead.
                Employee = payrollInvoice.Employee?.ToDto(),
                PeriodStart = payrollInvoice.PeriodStart,
                PeriodEnd = payrollInvoice.PeriodEnd
            },
            _ => throw new NotImplementedException($"Mapping for {entity.GetType().Name} is not implemented.")
        };
    }
}
