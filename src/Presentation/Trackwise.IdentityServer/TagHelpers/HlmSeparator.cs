using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-separator")]
public sealed class HlmSeparator() : HlmTagHelper("hlm-separator", "separator")
{
    protected override string Classes =>
        "inline-flex shrink-0 bg-border data-horizontal:h-px data-horizontal:w-full data-vertical:w-px data-vertical:self-stretch";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        base.Process(context, output);
        output.Attributes.SetAttribute("data-orientation", "horizontal");
    }
}
