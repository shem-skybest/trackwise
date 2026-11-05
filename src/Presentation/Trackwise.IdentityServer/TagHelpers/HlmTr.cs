using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement("tr", Attributes = "hlm-tr")]
public sealed class HlmTr() : HlmTagHelper("hlm-tr", "table-row")
{
    protected override string Classes => "spartan-table-row";
}
