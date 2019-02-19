using System.Web;
using Sitecore;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;

namespace Site.Foundation.Cache.Processor
{
	public class ApplyVaryByWebsite : RenderRenderingProcessor
	{
		public override void Process(RenderRenderingArgs args)
		{
			if (args.Rendered) return;

			if (HttpContext.Current == null || !args.Cacheable ||
				args.Rendering.RenderingItem?.InnerItem == null) return;

			var renderingItem = args.Rendering.RenderingItem.InnerItem;

			if (renderingItem["VaryByWebsite"] != "1") return;

			var currentItemId = Sitecore.Context.Site.Name.ToLower();
			args.CacheKey += "_#site:" + currentItemId;
		}
	}
}