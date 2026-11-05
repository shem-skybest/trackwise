using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-badge")]
public sealed class HlmBadge() : HlmTagHelper("hlm-badge", "badge")
{
    /// <summary>default, secondary, destructive or outline.</summary>
    public string Variant { get; set; } = "default";

    protected override string Classes => string.Join(' ',
        "spartan-badge group/badge inline-flex w-fit shrink-0 items-center justify-center overflow-hidden whitespace-nowrap",
        Variant switch
        {
            "secondary" => "spartan-badge-variant-secondary",
            "destructive" => "spartan-badge-variant-destructive",
            "outline" => "spartan-badge-variant-outline",
            _ => "spartan-badge-variant-default"
        });
}
