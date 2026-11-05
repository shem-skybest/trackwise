using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

[HtmlTargetElement(Attributes = "hlm-field")]
public sealed class HlmField() : HlmTagHelper("hlm-field", "field")
{
    /// <summary>vertical or horizontal.</summary>
    public string Orientation { get; set; } = "vertical";

    protected override string Classes => string.Join(' ',
        "spartan-field group/field flex w-full",
        Orientation == "horizontal"
            ? "spartan-field-orientation-horizontal flex-row items-center has-[>[data-slot=field-content]]:items-start has-[>[data-slot=field-content]]:[&>[role=checkbox],[role=radio]]:mt-px"
            : "spartan-field-orientation-vertical flex-col *:w-full [&>.sr-only]:w-auto");

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        base.Process(context, output);
        output.Attributes.SetAttribute("role", "group");
        output.Attributes.SetAttribute("data-orientation", Orientation);
    }
}
