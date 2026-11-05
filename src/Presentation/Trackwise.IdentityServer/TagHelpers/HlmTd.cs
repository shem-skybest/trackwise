using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement("td", Attributes = "hlm-td")]
public sealed class HlmTd() : HlmTagHelper("hlm-td", "table-cell")
{
    protected override string Classes => "spartan-table-cell";
}
