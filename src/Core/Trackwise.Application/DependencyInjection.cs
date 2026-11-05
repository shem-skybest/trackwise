using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;

namespace Trackwise.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFreightForwardingServices(this IServiceCollection services)
    {
        services.AddScoped<ITariffRateResolver, TariffRateResolver>();
        services.AddScoped<IQuotationRatingEngine, QuotationRatingEngine>();
        services.AddScoped<IQuotationReferenceGenerator, QuotationReferenceGenerator>();

        return services;
    }
}
