using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class CustomerMapper
{
    [MapperIgnoreSource(nameof(Customer.Invoices))]
    [MapperIgnoreSource(nameof(Customer.DomainEvents))]
    public static partial CustomerDto ToDto(this Customer entity);
}
