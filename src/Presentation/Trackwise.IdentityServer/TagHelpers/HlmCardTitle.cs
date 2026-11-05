using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-card-title")]
public sealed class HlmCardTitle() : HlmTagHelper("hlm-card-title", "card-title")
{
    protected override string Classes => "spartan-card-title";
}
