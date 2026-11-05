using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-card-description")]
public sealed class HlmCardDescription() : HlmTagHelper("hlm-card-description", "card-description")
{
    protected override string Classes => "spartan-card-description";
}
