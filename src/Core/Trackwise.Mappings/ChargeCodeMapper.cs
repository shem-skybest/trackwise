using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class ChargeCodeMapper
{
    [MapperIgnoreSource(nameof(ChargeCode.DomainEvents))]
    [MapperIgnoreSource(nameof(ChargeCode.Rates))]
    public static partial ChargeCodeDto ToDto(this ChargeCode entity);

    [MapperIgnoreSource(nameof(ChargeCode.DomainEvents))]
    [MapperIgnoreSource(nameof(ChargeCode.Rates))]
    [MapperIgnoreSource(nameof(ChargeCode.Description))]
    [MapperIgnoreSource(nameof(ChargeCode.RequiresApproval))]
    [MapperIgnoreSource(nameof(ChargeCode.TaxCode))]
    [MapperIgnoreSource(nameof(ChargeCode.Currency))]
    [MapperIgnoreSource(nameof(ChargeCode.TenantId))]
    [MapperIgnoreSource(nameof(ChargeCode.CreatedById))]
    [MapperIgnoreSource(nameof(ChargeCode.CreatedAt))]
    [MapperIgnoreSource(nameof(ChargeCode.ApplicableModes))]
    public static partial ChargeCodeSummaryDto ToSummaryDto(this ChargeCode entity);

    public static partial List<ChargeCodeDto> ToDtoList(this IEnumerable<ChargeCode> entities);

    public static partial List<ChargeCodeSummaryDto> ToSummaryDtoList(this IEnumerable<ChargeCode> entities);

    /// <summary>
    /// Creates a new ChargeCode via the domain factory. TenantId and CreatedById are not
    /// parameters on ChargeCode.Create — see note below; wire these up once the factory/entity
    /// exposes a way to set them.
    /// </summary>
    public static ChargeCode ToEntity(this CreateChargeCodeRequest request)
    {
        return ChargeCode.Create(
            code: request.Code,
            name: request.Name,
            category: request.Category,
            modes: request.ApplicableModes,
            defaultUnit: request.DefaultUnit,
            requiresApproval: request.RequiresApproval,
            description: request.Description);
    }

    /// <summary>
    /// Applies an update request onto an existing ChargeCode via its domain method.
    /// </summary>
    public static void ApplyUpdate(this ChargeCode entity, UpdateChargeCodeRequest request)
    {
        entity.UpdateDetails(
            name: request.Name,
            description: request.Description,
            category: request.Category,
            modes: request.ApplicableModes,
            defaultUnit: request.DefaultUnit,
            requiresApproval: request.RequiresApproval,
            taxCode: request.TaxCode);
    }
}
