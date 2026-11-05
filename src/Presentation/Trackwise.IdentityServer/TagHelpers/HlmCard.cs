using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-card")]
public sealed class HlmCard() : HlmTagHelper("hlm-card", "card")
{
    protected override string Classes => "spartan-card group/card flex flex-col";
}
