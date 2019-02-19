using System.Web;
using Sitecore;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;

namespace Site.Foundation.Cache.Processor
{
	public class ApplyVaryByUrlCaching : RenderRenderingProcessor
	{
		public override void Process(RenderRenderingArgs args)
		{
			if (args.Rendered) return;

			if (HttpContext.Current == null || !args.Cacheable ||
				args.Rendering.RenderingItem?.InnerItem == null) return;

			var renderingItem = args.Rendering.RenderingItem.InnerItem;

			if (renderingItem["VaryByUrl"] != "1") return;

			var currentItemId = Context.Item.ID.ToShortID();
			args.CacheKey += "_#ciid:" + currentItemId;
		}
	}
}