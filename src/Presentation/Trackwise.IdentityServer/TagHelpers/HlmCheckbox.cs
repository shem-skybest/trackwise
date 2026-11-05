using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

/// <summary>A native checkbox drawn as spartan's; the tick comes from wwwroot/img/check.svg (see Styles/app.css).</summary>
[HtmlTargetElement("input", Attributes = "hlm-checkbox")]
public sealed class HlmCheckbox() : HlmTagHelper("hlm-checkbox", "checkbox")
{
    protected override string Classes =>
        "spartan-checkbox peer shrink-0 cursor-default outline-none disabled:cursor-not-allowed disabled:opacity-50";
}
