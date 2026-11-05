using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Trackwise.IdentityServer.TagHelpers;

/// <summary>Goes on an <c>asp-validation-for</c> span; Styles/app.css hides it while the field is valid.</summary>
[HtmlTargetElement(Attributes = "hlm-field-error")]
public sealed class HlmFieldError() : HlmTagHelper("hlm-field-error", "field-error")
{
    protected override string Classes => "spartan-field-error font-normal";
}
