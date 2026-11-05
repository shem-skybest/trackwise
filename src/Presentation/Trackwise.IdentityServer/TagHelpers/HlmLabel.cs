using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-label")]
public sealed class HlmLabel() : HlmTagHelper("hlm-label", "label")
{
    protected override string Classes =>
        "spartan-label flex items-center select-none group-data-[disabled=true]:pointer-events-none peer-disabled:cursor-not-allowed";
}
