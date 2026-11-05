using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement("thead", Attributes = "hlm-thead")]
public sealed class HlmTHead() : HlmTagHelper("hlm-thead", "table-header")
{
    protected override string Classes => "spartan-table-header";
}
