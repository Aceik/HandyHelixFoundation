using Site.Foundation.Analytics.Media;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Site.Foundation.Analytics.Processor
{
    // Uses the adapative images package to pick up variation necessary for adapative images and cache combinations:
    // https://github.com/scottmulligan/SitecoreAdaptiveImages
    public class ApplyVaryByResolution : RenderRenderingProcessor
    {
        public override void Process(RenderRenderingArgs args)
        {
            if (args.Rendered) return;

            if (HttpContext.Current == null || !args.Cacheable ||
                args.Rendering.RenderingItem?.InnerItem == null) return;

            var renderingItem = args.Rendering.RenderingItem.InnerItem;

            if (renderingItem["VaryByResolution"] != "1") return;

            if (!AdaptiveMediaProvider.IsResolutionCookieSet())
            {
                // if no cookie set, it is better use largest resolution
                args.CacheKey += "_#resolution:" + AdaptiveMediaProvider.GetLargestBreakpoint();
            }
            else if (AdaptiveMediaProvider.GetScreenResolution() != 0)
            {
                args.CacheKey += "_#resolution:" + AdaptiveMediaProvider.GetScreenResolution();
            }
            // else if screen is too big for preset resolution (resolution will be 0), 
            // return cached version of original image (without resolution cache key)
        }
    }
}