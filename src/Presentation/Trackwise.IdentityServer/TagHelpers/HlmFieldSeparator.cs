using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

/// <summary>
/// <c>&lt;hlm-field-separator&gt;Or continue with&lt;/hlm-field-separator&gt;</c>, rendered with the
/// same inner structure as helm's component template.
/// </summary>
[HtmlTargetElement("hlm-field-separator")]
public sealed class HlmFieldSeparator() : HlmTagHelper("hlm-field-separator", "field-separator")
{
    protected override string Classes => "spartan-field-separator relative";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        base.Process(context, output);
        output.PreContent.SetHtmlContent(
            """<div data-slot="separator" data-orientation="horizontal" class="inline-flex shrink-0 bg-border data-horizontal:h-px data-horizontal:w-full absolute inset-0 top-1/2"></div><span data-slot="field-separator-content" class="spartan-field-separator-content bg-background relative mx-auto block w-fit">""");
        output.PostContent.SetHtmlContent("</span>");
    }
}
