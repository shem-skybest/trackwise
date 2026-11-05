using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

public class FxRateOptions
{
    public const string SectionName = "FxRates";

    public string BaseUrl { get; set; } = "https://api.currencylayer.com/";
    public string AccessKey { get; set; } = default!;
    public int CacheDurationHours { get; set; } = 4;
}
