using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-field-description")]
public sealed class HlmFieldDescription() : HlmTagHelper("hlm-field-description", "field-description")
{
    protected override string Classes =>
        "spartan-field-description leading-normal font-normal group-has-data-horizontal/field:text-balance last:mt-0 nth-last-2:-mt-1 [&>a:hover]:text-primary [&>a]:underline [&>a]:underline-offset-4";
}
