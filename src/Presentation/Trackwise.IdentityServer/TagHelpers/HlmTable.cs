using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement("table", Attributes = "hlm-table")]
public sealed class HlmTable() : HlmTagHelper("hlm-table", "table")
{
    protected override string Classes => "spartan-table";
}
