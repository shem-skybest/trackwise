using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

/// <summary>
/// Razor counterparts of spartan's helm directives. Each puts the classes and <c>data-slot</c> its
/// Angular directive puts on its host, so a page reads like the spartan block it was taken from:
/// <c>&lt;button hlm-btn variant="outline"&gt;</c>. Class strings are copied from spartan's
/// <c>libs/helm</c> at the commit named in Styles/spartan-vega.css, which defines the <c>spartan-*</c>
/// classes. Where helm targets <c>ng-icon</c>, these target <c>svg</c>, since the pages inline icons.
/// </summary>
/// <remarks>
/// Tailwind scans this folder (Styles/app.css <c>@source</c>), so every class must appear in it as a
/// complete literal - never build one by concatenation.
/// </remarks>
public abstract class HlmTagHelper(string attribute, string slot) : TagHelper
{
    protected abstract string Classes { get; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll(attribute);
        output.Attributes.SetAttribute("data-slot", slot);
        foreach (var cssClass in Classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            output.AddClass(cssClass, HtmlEncoder.Default);
        }
    }
}
