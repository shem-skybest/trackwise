using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-alert-description")]
public sealed class HlmAlertDescription() : HlmTagHelper("hlm-alert-description", "alert-description")
{
    protected override string Classes => "spartan-alert-description [&_a]:hover:text-foreground [&_a]:underline [&_a]:underline-offset-3";
}
