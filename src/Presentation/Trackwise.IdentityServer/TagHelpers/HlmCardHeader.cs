using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-card-header")]
public sealed class HlmCardHeader() : HlmTagHelper("hlm-card-header", "card-header")
{
    protected override string Classes =>
        "spartan-card-header group/card-header @container/card-header grid auto-rows-min items-start has-data-[slot=card-action]:grid-cols-[1fr_auto] has-data-[slot=card-description]:grid-rows-[auto_auto]";
}
