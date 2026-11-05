using Trackwise.Application.Abstractions.Privacy;
using Trackwise.Infrastructure.Documents.Privacy;
using Trackwise.Infrastructure.Documents.Pdf;
using Trackwise.Infrastructure.Documents.PdfImport;
using Microsoft.Extensions.DependencyInjection;
using Trackwise.Application.Abstractions.Documents;

namespace Trackwise.Infrastructure.Documents;

public static class Registrar
{
    /// <summary>
    ///     Add document infrastructure (PDF generation/import, GDPR data export).
    /// </summary>
    public static IServiceCollection AddDocumentsInfrastructure(this IServiceCollection services)
    {
        // PDF generation
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();
        services.AddScoped<IPayrollPayStubService, PayrollPayStubService>();
        services.AddScoped<IIftaReportPdfService, Pdf.Ifta.IftaReportPdfService>();
        services.AddScoped<IBillingBatchPdfService, Pdf.Billing.BillingBatchPdfService>();
        services.AddScoped<IQuotationPdfGenerator, Pdf.Quotations.QuotationPdfGenerator>();
        services.AddScoped<IBookingPdfGenerator, Pdf.Bookings.BookingPdfGenerator>();

        // PDF import (LLM-based extraction)
        services.AddScoped<IPdfDataExtractor, LlmPdfDataExtractor>();

        // Privacy / GDPR
        services.AddScoped<IDataExportService, DataExportService>();
        return services;
    }
}
