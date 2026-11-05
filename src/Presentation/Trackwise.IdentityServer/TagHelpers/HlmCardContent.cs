using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-card-content")]
public sealed class HlmCardContent() : HlmTagHelper("hlm-card-content", "card-content")
{
    protected override string Classes => "spartan-card-content";
}
