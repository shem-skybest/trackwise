using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement("button", Attributes = "hlm-btn")]
[HtmlTargetElement("a", Attributes = "hlm-btn")]
public sealed class HlmButton() : HlmTagHelper("hlm-btn", "button")
{
    /// <summary>default, outline, secondary, ghost, destructive or link.</summary>
    public string Variant { get; set; } = "default";

    /// <summary>default, xs, sm, lg or icon.</summary>
    public string Size { get; set; } = "default";

    protected override string Classes => string.Join(' ',
        "spartan-button group/button inline-flex shrink-0 items-center justify-center whitespace-nowrap transition-all outline-none select-none disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
        Variant switch
        {
            "outline" => "spartan-button-variant-outline",
            "secondary" => "spartan-button-variant-secondary",
            "ghost" => "spartan-button-variant-ghost",
            "destructive" => "spartan-button-variant-destructive",
            "link" => "spartan-button-variant-link",
            _ => "spartan-button-variant-default"
        },
        Size switch
        {
            "xs" => "spartan-button-size-xs",
            "sm" => "spartan-button-size-sm",
            "lg" => "spartan-button-size-lg",
            "icon" => "spartan-button-size-icon",
            _ => "spartan-button-size-default"
        });
}
