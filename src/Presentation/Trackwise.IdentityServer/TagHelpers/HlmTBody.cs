using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement("tbody", Attributes = "hlm-tbody")]
public sealed class HlmTBody() : HlmTagHelper("hlm-tbody", "table-body")
{
    protected override string Classes => "spartan-table-body";
}
