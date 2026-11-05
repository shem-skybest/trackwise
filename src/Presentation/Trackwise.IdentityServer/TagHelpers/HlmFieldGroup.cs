using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-field-group")]
public sealed class HlmFieldGroup() : HlmTagHelper("hlm-field-group", "field-group")
{
    protected override string Classes => "spartan-field-group group/field-group @container/field-group flex w-full flex-col";
}
