using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement("input", Attributes = "hlm-input")]
public sealed class HlmInput() : HlmTagHelper("hlm-input", "input")
{
    protected override string Classes =>
        "spartan-input file:text-foreground placeholder:text-muted-foreground w-full min-w-0 outline-none file:inline-flex file:border-0 file:bg-transparent disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50";
}
