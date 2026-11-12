using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.AI;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services.BookingRequests;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class ReadBookingRequestHandler(
    ITenantUnitOfWork tenantUow,
    ILlmClient llmClient,
    ILogger<ReadBookingRequestHandler> logger)
    : IAppRequestHandler<ReadBookingRequestQuery, Result<BookingRequestReadingDto>>
{
    // Frabelle alone has ~900 outlets; the whole list still fits comfortably in one request.
    private const int MaxSites = 1500;
    private const int MaxOutputTokens = 4096;

    private static readonly LocationType[] SharedPickupTypes =
        [LocationType.Warehouse, LocationType.Plant, LocationType.Depot, LocationType.ContainerYard];

    public async Task<Result<BookingRequestReadingDto>> Handle(ReadBookingRequestQuery req, CancellationToken ct)
    {
        var customer = await tenantUow.Repository<Customer>().Query().AsNoTracking()
            .Where(c => c.Id == req.CustomerId)
            .Select(c => new { c.Id, c.Name })
            .FirstOrDefaultAsync(ct);
        if (customer is null)
            return Result<BookingRequestReadingDto>.Fail("Client not found.");

        var sites = await tenantUow.Repository<Location>().Query().AsNoTracking()
            .Where(l => l.IsActive && l.DeletedAt == null && l.MergedIntoId == null && l.Type != LocationType.Zone)
            .Where(l => l.CustomerId == req.CustomerId
                || (l.CustomerId == null && SharedPickupTypes.Contains(l.Type)))
            .OrderBy(l => l.CustomerId == null)
            .ThenBy(l => l.Name)
            .Take(MaxSites)
            .Select(l => new SiteRow(
                l.Id, l.Name, l.Code, l.Type, l.CustomerId != null,
                l.Address != null ? l.Address.CityOrMunicipality : null))
            .ToListAsync(ct);

        // Short keys rather than GUIDs: cheaper, and the model copies them back reliably.
        var sitesByKey = sites
            .Select((s, i) => (Key: SiteKey(i), Site: s))
            .ToDictionary(
                x => x.Key,
                x => new ProposedSiteDto { Id = x.Site.Id, Name = x.Site.Name, Code = x.Site.Code, City = x.Site.City });

        var request = new LlmCompletionRequest
        {
            SystemPrompt = BookingRequestPrompt.System,
            UserText = BuildUserText(req, customer.Name, sites),
            Documents = req.Attachment is { Length: > 0 }
                ? [new LlmInlineDocument(req.AttachmentMediaType!.ToLowerInvariant(), req.Attachment)]
                : [],
            MaxTokens = MaxOutputTokens
        };

        var completion = await llmClient.CompleteAsync(request, ct);
        if (!completion.IsSuccess)
        {
            logger.LogWarning("Reading a booking request for customer {CustomerId} failed: {Error}",
                req.CustomerId, completion.Error);
            return Result<BookingRequestReadingDto>.Fail(
                $"The AI could not read the request ({completion.Error}). Type the booking in by hand.");
        }

        logger.LogInformation(
            "Read booking request for customer {CustomerId} with {Model}: {Input} in / {Output} out tok, est ${Cost:F4}",
            req.CustomerId, completion.Value!.ModelUsed, completion.Value.InputTokens,
            completion.Value.OutputTokens, completion.Value.EstimatedCostUsd);

        var reading = BookingRequestReplyParser.Parse(completion.Value.Text, sitesByKey);
        if (reading.IsSuccess && reading.Value!.Trips.Count == 0 && reading.Value.Warnings.Count == 0)
            reading.Value.Warnings.Add("No truck request was found in this message.");

        return reading;
    }

    private static string SiteKey(int index) => $"S{index + 1}";

    private static string BuildUserText(ReadBookingRequestQuery req, string customerName, List<SiteRow> sites)
    {
        var text = new StringBuilder();
        text.AppendLine($"Client: {customerName}");
        text.AppendLine(
            $"Received: {req.ReceivedOn.ToString("yyyy-MM-dd (dddd)", CultureInfo.InvariantCulture)}");
        text.AppendLine();

        text.AppendLine("Saved locations (key | name | code | type | city | owner):");
        if (sites.Count == 0)
            text.AppendLine("(none saved yet)");

        for (var i = 0; i < sites.Count; i++)
        {
            var site = sites[i];
            var owner = site.IsClientOwn ? "client" : "shared";
            text.AppendLine($"{SiteKey(i)} | {site.Name} | {site.Code} | {site.Type} | {site.City} | {owner}");
        }

        text.AppendLine();
        if (!string.IsNullOrWhiteSpace(req.MessageText))
        {
            text.AppendLine("The client's message:");
            text.AppendLine(req.MessageText.Trim());
        }

        if (req.Attachment is { Length: > 0 })
            text.AppendLine("The client's request is attached.");

        return text.ToString();
    }

    private sealed record SiteRow(
        Guid Id, string Name, string? Code, LocationType Type, bool IsClientOwn, string? City);
}
