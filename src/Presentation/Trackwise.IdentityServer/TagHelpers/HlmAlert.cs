using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-alert")]
public sealed class HlmAlert() : HlmTagHelper("hlm-alert", "alert")
{
    /// <summary>default or destructive.</summary>
    public string Variant { get; set; } = "default";

    protected override string Classes => Variant == "destructive"
        ? "spartan-alert group/alert relative w-full spartan-alert-variant-destructive"
        : "spartan-alert group/alert relative w-full spartan-alert-variant-default";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        base.Process(context, output);
        output.Attributes.SetAttribute("role", "alert");
    }
}
