using System.Web;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;

namespace Site.Foundation.Analytics.Processor
{
	public class ApplyVaryByRawUrlCaching : RenderRenderingProcessor
	{
		public override void Process(RenderRenderingArgs args)
		{
			if (args.Rendered) return;

			if (HttpContext.Current == null || !args.Cacheable ||
				args.Rendering.RenderingItem?.InnerItem == null) return;

			var renderingItem = args.Rendering.RenderingItem.InnerItem;

			if (renderingItem["VaryByRawUrl"] != "1") return;
                                                              
			args.CacheKey += "_#rawurl:" + HttpContext.Current.Request.RawUrl;
		}
	}
}