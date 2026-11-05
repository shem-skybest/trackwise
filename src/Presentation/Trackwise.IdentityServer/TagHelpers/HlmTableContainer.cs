using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement("div", Attributes = "hlm-table-container")]
public sealed class HlmTableContainer() : HlmTagHelper("hlm-table-container", "table-container")
{
    protected override string Classes => "spartan-table-container";
}
