using FleetManager.Business.UtilityModels.CommonSecurity;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace FleetManager.App.CustomTagHelper
{
    /// <summary>
    /// Usage:
    ///   <a asp-action="Details" asp-controller="FuelLog" asp-encoded-id="@item.Id">Details</a>
    ///
    /// This TagHelper runs *before* the built-in AnchorTagHelper (Order = -1000),
    /// sets asp-route-id to the protected value, then removes the asp-encoded-id attribute.
    /// </summary>
    [HtmlTargetElement("a", Attributes = "asp-protect-route-id")]
    [HtmlTargetElement("form", Attributes = "asp-protect-route-id")]
    public class ProtectRouteIdTagHelper : AnchorTagHelper
    {
        private readonly IIdProtector _idProtector;

        public ProtectRouteIdTagHelper(IHtmlGenerator generator, IIdProtector idProtector)
            : base(generator)
        {
            _idProtector = idProtector ?? throw new ArgumentNullException(nameof(idProtector));
        }

        /// <summary>
        /// The unprotected ID to encrypt before generating the URL.
        /// Accepts string (GUID), numeric values converted to string, etc.
        /// </summary>
        [HtmlAttributeName("asp-protect-route-id")]
        public object? ProtectRouteId { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Normalize incoming id to string
            string? idString = null;
            if (ProtectRouteId != null)
            {
                switch (ProtectRouteId)
                {
                    case long l:
                        idString = l.ToString();
                        break;
                    case int i:
                        idString = i.ToString();
                        break;
                    case Guid g:
                        idString = g.ToString();
                        break;
                    default:
                        idString = ProtectRouteId.ToString();
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(idString))
            {
                // Protect via IIdProtector - expects ProtectIdForAny to exist
                string protectedId;
                try
                {
                    protectedId = _idProtector.ProtectIdForAny(idString);
                }
                catch
                {
                    // fallback to raw idString if protection fails (avoid breaking links)
                    protectedId = idString;
                }

                // set the route value for id so AnchorTagHelper will include it
                // Note: RouteValues comes from AnchorTagHelper base class
                if (RouteValues != null)
                {
                    RouteValues["id"] = protectedId;
                }
            }

            // If a literal href was already present in the tag, AnchorTagHelper will throw when it sees
            // asp-* attributes. Remove literal href so base.Process can generate the correct href.
            // This overrides any literal href — make sure you don't intentionally want both.
            if (output.Attributes.ContainsName("href"))
            {
                output.Attributes.RemoveAll("href");
            }

            // Remove our custom attribute so it doesn't appear in the final HTML
            output.Attributes.RemoveAll("asp-protect-route-id");

            // Now let AnchorTagHelper do its job and generate the final href using route values
            base.Process(context, output);
        }
    }
}
