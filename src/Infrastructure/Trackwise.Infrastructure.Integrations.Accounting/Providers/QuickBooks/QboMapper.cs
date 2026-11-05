using System.Globalization;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.Integrations.Accounting.Providers.QuickBooks;

/// <summary>
///     Maps provider-neutral push payloads to QuickBooks Online wire models. When an existing
///     <c>qboId</c>/<c>syncToken</c> is supplied, the resulting object carries them so QBO
///     performs a full update rather than a create.
/// </summary>
internal static class QboMapper
{
    private static string Date(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static QboCustomer MapCustomer(QboCustomerPayload p, string? qboId, string? syncToken)
    {
        return new QboCustomer
        {
            Id = qboId,
            SyncToken = syncToken,
            DisplayName = p.Name,
            PrimaryEmailAddr = string.IsNullOrWhiteSpace(p.Email) ? null : new QboEmail { Address = p.Email },
            PrimaryPhone = string.IsNullOrWhiteSpace(p.Phone) ? null : new QboPhone { FreeFormNumber = p.Phone },
            BillAddr = MapAddress(p.BillingAddress)
        };
    }

    private static QboPhysicalAddress? MapAddress(Address? a)
    {
        if (a is null)
        {
            return null;
        }

        return new QboPhysicalAddress
        {
            Line1 = a.StreetAddress,
            Line2 = a.Subdivision,
            City = a.CityOrMunicipality,
            CountrySubDivisionCode = a.Province,
            PostalCode = a.Zipcode,
            Country = a.Country
        };
    }

    public static QboInvoice MapInvoice(QboInvoicePayload p, string? qboId, string? syncToken)
    {
        var lines = p.Lines
            .Select(l => new QboLine
            {
                DetailType = "SalesItemLineDetail",
                Description = l.Description,
                Amount = decimal.Round(l.Amount * l.Quantity, 2),
                SalesItemLineDetail = new QboSalesItemLineDetail
                {
                    Qty = l.Quantity,
                    UnitPrice = decimal.Round(l.Amount, 2)
                }
            })
            .ToList();

        return new QboInvoice
        {
            Id = qboId,
            SyncToken = syncToken,
            DocNumber = p.DocNumber,
            CustomerRef = QboRef.Of(p.QboCustomerId),
            CurrencyRef = QboRef.Of(p.CurrencyCode),
            DueDate = p.DueDate.HasValue ? Date(p.DueDate.Value) : null,
            TxnTaxDetail = p.TaxTotal > 0 ? new QboTxnTaxDetail { TotalTax = decimal.Round(p.TaxTotal, 2) } : null,
            Line = lines
        };
    }

    public static QboPayment MapPayment(QboPaymentPayload p, string? qboId, string? syncToken)
    {
        return new QboPayment
        {
            Id = qboId,
            SyncToken = syncToken,
            CustomerRef = QboRef.Of(p.QboCustomerId),
            CurrencyRef = QboRef.Of(p.CurrencyCode),
            TotalAmt = decimal.Round(p.Amount, 2),
            PaymentRefNum = p.ReferenceNumber,
            Line =
            [
                new QboLine
                {
                    Amount = decimal.Round(p.Amount, 2),
                    LinkedTxn = [new QboLinkedTxn { TxnId = p.QboInvoiceId, TxnType = "Invoice" }]
                }
            ]
        };
    }

    public static QboPurchase MapPurchase(QboExpensePayload p, string? qboId, string? syncToken)
    {
        return new QboPurchase
        {
            Id = qboId,
            SyncToken = syncToken,
            AccountRef = QboRef.Of(p.PaymentAccountId),
            CurrencyRef = QboRef.Of(p.CurrencyCode),
            PaymentType = "Cash",
            TxnDate = Date(p.TxnDate),
            Line =
            [
                new QboLine
                {
                    DetailType = "AccountBasedExpenseLineDetail",
                    Description = p.Description,
                    Amount = decimal.Round(p.Amount, 2),
                    AccountBasedExpenseLineDetail = new QboAccountBasedExpenseLineDetail
                    {
                        AccountRef = QboRef.Of(p.ExpenseAccountId)
                    }
                }
            ]
        };
    }
}
