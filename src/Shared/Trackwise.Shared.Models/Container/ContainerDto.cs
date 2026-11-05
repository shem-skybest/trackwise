using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

public class ContainerDto
{
    public Guid Id { get; set; }
    public required string Number { get; set; }
    public Guid ContainerTypeId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? ContainerTypeCode { get; set; }

    public string? ContainerTypeName { get; set; }
    public bool IsLaden { get; set; }
    public ContainerStatus Status { get; set; }
    public Guid? CurrentTerminalId { get; set; }
    public string? CurrentTerminalName { get; set; }
    public string? CurrentTerminalCode { get; set; }
    public string? Notes { get; set; }
    public DateTime? LoadedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>The workfiles this box has shipped on, newest first. Filled on the detail read only.</summary>
    public List<ContainerShipmentDto> Shipments { get; set; } = [];
}

/// <summary>One move of a container: the workfile, and what was true of the box on it.</summary>
public class ContainerShipmentDto
{
    public Guid WorkfileId { get; set; }
    public string ReferenceNumber { get; set; } = default!;
    public string? BillOfLadingNumber { get; set; }
    public WorkfileStatus Status { get; set; }
    public string? SealNumber { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public DateTime CreatedAt { get; set; }
}
