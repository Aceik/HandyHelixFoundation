using Site.Foundation.Analytics.Media;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Site.Foundation.Analytics.Processor
{
    // Credits: https://sitecoremaster.com/caching/customizing-html-caching-with-sitecore-with-mvc/
    public class ApplyVaryByTimeout : AddRecordedHtmlToCache
    {
        protected override TimeSpan GetTimeout(RenderRenderingArgs args)
        {
            TimeSpan result;
            return TimeSpan.TryParse(args.Rendering.RenderingItem.InnerItem["Timeout"], out result) ? result : args.Rendering.Caching.Timeout;
        }

        public override void Process(RenderRenderingArgs args)
        {
            if (args.Rendered) return;

            if (HttpContext.Current == null || !args.Cacheable ||
                args.Rendering.RenderingItem?.InnerItem == null) return;

            var renderingItem = args.Rendering.RenderingItem.InnerItem;

            if (renderingItem["VaryByTimeout"] != "1") return;
            
            base.Process(args);
        }
    }
}