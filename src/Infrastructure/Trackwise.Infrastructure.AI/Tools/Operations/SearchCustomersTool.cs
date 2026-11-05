using System.ComponentModel;
using Trackwise.Application.Abstractions.Agents;
using Trackwise.Application.Modules.IdentityAccess.Customers.Queries;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Policies;
using MediatR;

namespace Trackwise.Infrastructure.AI.Tools.Operations;

internal sealed class SearchCustomersTool(IMediator mediator)
    : AgentTool<SearchCustomersTool.Input>, IAgentToolMetadata
{
    internal sealed record Input
    {
        [Description("Customer name or name fragment (case-sensitive substring match)")]
        public string? Search { get; init; }
    }

    public static AgentToolDefinition Definition => new(
        "search_customers",
        "Search customers by name. The match is a case-sensitive substring - if nothing is found, retry with a shorter fragment (e.g. 'Acme' instead of 'acme logistics'). Returns up to 20 customers with ID, name, email, and phone.")
    {
        RequiredFeature = TenantFeature.Customers,
        RequiredPermission = Permission.Customer.View,
        Surfaces = AgentSurfaces.Copilot | AgentSurfaces.Mcp
    };


    protected override async Task<string> ExecuteAsync(Input input, CancellationToken ct)
    {
        var query = new GetCustomersQuery
        {
            Search = input.Search,
            Page = 1,
            PageSize = ToolResult.MaxResults
        };

        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
            return ToolResult.Error(result.Error);

        var items = result.Value?.ToList() ?? [];
        var customers = items.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            email = c.Email,
            phone = c.Phone,
            status = c.Status.ToString()
        }).ToList();

        return ToolResult.Paged("customers", customers, result.TotalItems);
    }
}
