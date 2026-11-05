using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

/// <summary>HlmFieldLabel carries HlmLabel as a host directive, so it gets both class sets.</summary>
[HtmlTargetElement(Attributes = "hlm-field-label")]
public sealed class HlmFieldLabel() : HlmTagHelper("hlm-field-label", "field-label")
{
    protected override string Classes =>
        "spartan-label flex items-center select-none group-data-[disabled=true]:pointer-events-none peer-disabled:cursor-not-allowed " +
        "spartan-field-label group/field-label peer/field-label w-fit has-[>[data-slot=field]]:w-full has-[>[data-slot=field]]:flex-col";
}
