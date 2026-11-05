using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement("textarea", Attributes = "hlm-textarea")]
public sealed class HlmTextarea() : HlmTagHelper("hlm-textarea", "textarea")
{
    protected override string Classes =>
        "spartan-textarea placeholder:text-muted-foreground flex field-sizing-content min-h-16 w-full outline-none disabled:cursor-not-allowed disabled:opacity-50";
}
