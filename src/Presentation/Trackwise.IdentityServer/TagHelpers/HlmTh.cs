using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement("th", Attributes = "hlm-th")]
public sealed class HlmTh() : HlmTagHelper("hlm-th", "table-head")
{
    protected override string Classes => "spartan-table-head";
}
