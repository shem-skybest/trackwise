using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-alert-title")]
public sealed class HlmAlertTitle() : HlmTagHelper("hlm-alert-title", "alert-title")
{
    protected override string Classes => "spartan-alert-title [&_a]:hover:text-foreground [&_a]:underline [&_a]:underline-offset-3";
}
