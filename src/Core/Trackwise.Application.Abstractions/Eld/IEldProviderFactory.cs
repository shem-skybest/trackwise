using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Application.Abstractions.Eld;

namespace Trackwise.Application.Abstractions.Eld;

/// <summary>
/// Factory for creating ELD provider service instances
/// </summary>
public interface IEldProviderFactory
{
    /// <summary>
    /// Get an ELD provider service by type
    /// </summary>
    IEldProviderService GetProvider(EldProviderType providerType);

    /// <summary>
    /// Get an ELD provider service initialized with the given configuration
    /// </summary>
    IEldProviderService GetProvider(EldProviderConfiguration configuration);

    /// <summary>
    /// Check if a provider type is supported
    /// </summary>
    bool IsProviderSupported(EldProviderType providerType);
}
