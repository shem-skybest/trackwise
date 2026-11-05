using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-card-footer")]
public sealed class HlmCardFooter() : HlmTagHelper("hlm-card-footer", "card-footer")
{
    protected override string Classes => "spartan-card-footer flex items-center";
}
