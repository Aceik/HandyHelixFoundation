using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Site.Feature.Analytics.Helper
{
    public class CrossSiteTrackingHelper
    {
        // Need to support both Content-Security-Policy  and  X-FRAME-OPTIONS

        //As per the MDN Specifications, X-Frame-Options: ALLOW-FROM is not supported in Chrome and support is unknown in Edge and Opera.
        //Content-Security-Policy: frame-ancestors overrides X-Frame-Options (as per this W3 spec), but frame-ancestors has limited compatibility. As per these MDN Specs, it's not supported in IE or Edge.

        // https://stackoverflow.com/questions/10205192/x-frame-options-allow-from-multiple-domains
        public static string SetupIFrameSecurity()
        {
            var domains = Sitecore.Configuration.Settings.GetSetting("Sitecore.Foundation.Analytics.CookieDomains");
            var domainsStripped = domains.Replace("'", string.Empty).Replace(",", string.Empty);

            var response = HttpContext.Current.Response;
            var browserType = HttpContext.Current.Request.Browser.Type.ToLower();
            var isIe = HttpContext.Current.Request.UserAgent.Contains("Edge") || browserType.Contains("ie") || browserType.Contains("internetexplorer");
            if (isIe)
            {
                var domainCurrent = HttpContext.Current.Request.Url.Host.ToLower();
                var optionsValue = string.Empty;
                if (domainsStripped.Contains(domainCurrent))
                {
                    optionsValue = "allow";
                }
                else
                {
                    optionsValue = "Deny";
                }
                HttpContext.Current.Response.Headers["X-Frame-Options"] = optionsValue;
            }
            else
            {
                response.AddHeader("Content-Security-Policy", "frame-ancestors 'self' " + domainsStripped + " ;");
            }
            return domains;
        }
    }
}