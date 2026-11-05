using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// Read model for a ChargeCode.
/// </summary>
public class ChargeCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public ChargeCategory Category { get; set; }
    public FreightMode[] ApplicableModes { get; set; } = [];
    public string DefaultUnit { get; set; } = default!;
    public bool RequiresApproval { get; set; }
    public string? TaxCode { get; set; }
    public string? Currency { get; set; }
    public bool IsActive { get; set; }
    public Guid TenantId { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Lightweight version for dropdowns/pickers (e.g., selecting a charge code when adding a TariffRate).
/// </summary>
public class ChargeCodeSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ChargeCategory Category { get; set; }
    public string DefaultUnit { get; set; } = default!;
    public bool IsActive { get; set; }
}

/// <summary>
/// Request body for creating a new ChargeCode (maps to ChargeCode.Create).
/// </summary>
public class CreateChargeCodeRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ChargeCategory Category { get; set; }
    public FreightMode[] ApplicableModes { get; set; } = [];
    public string DefaultUnit { get; set; } = default!;
    public bool RequiresApproval { get; set; } = false;
    public string? Description { get; set; }
    // TenantId and CreatedById should come from the authenticated user/tenant context, not the request body
}

/// <summary>
/// Request body for updating an existing ChargeCode (maps to ChargeCode.UpdateDetails).
/// All fields optional — only provided values are applied.
/// </summary>
public class UpdateChargeCodeRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ChargeCategory? Category { get; set; }
    public FreightMode[]? ApplicableModes { get; set; }
    public string? DefaultUnit { get; set; }
    public bool? RequiresApproval { get; set; }
    public string? TaxCode { get; set; }
}
