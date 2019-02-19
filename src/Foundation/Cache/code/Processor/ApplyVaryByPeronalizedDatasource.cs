using Sitecore.Mvc.Pipelines.Response.RenderRendering;
using Sitecore.Mvc.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Mvc.Extensions;
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.Layouts;
using Sitecore.Data;
using Sitecore.Data.Items;

/// <summary>
/// Reference: http://www.sitecorecoding.com/2016/02/caching-rendering-html-when-conditional.html
/// </summary>
namespace Site.Foundation.Cache.Processor
{
	public class ApplyVaryByPeronalizedDatasource : RenderRenderingProcessor
	{
		public override void Process(RenderRenderingArgs args)
		{
			if (args.Rendered) return;

			if (HttpContext.Current == null || !args.Cacheable ||
				args.Rendering.RenderingItem?.InnerItem == null) return;

			var renderingItem = args.Rendering.RenderingItem.InnerItem;

			if (renderingItem?["VaryByPersonalizedData"] != "1") return;

			var allReferences = GetRenderingsForControl(Sitecore.Context.Item, Sitecore.Context.Device).ToList();
			var renderingUniqueId = ID.Parse(args.Rendering.UniqueId);
			var renderingReferrence = allReferences.Where(i => ID.Parse(i.UniqueId).Equals(renderingUniqueId)).FirstOrDefault();
			if (renderingReferrence != null)
			{
				var ruleContext = new ConditionalRenderingsRuleContext(allReferences, renderingReferrence);
				renderingReferrence.Settings.Rules.RunFirstMatching(ruleContext);
				if (!ruleContext.Reference.Settings.Rules.Rules.Any())
					return;
				
				string personalizedDatasource = ruleContext.Reference.Settings.DataSource;

				args.CacheKey += String.Concat("_#personlizedData:", personalizedDatasource);
			}
		}

		private RenderingReference[] GetRenderingsForControl(Item contextItem, DeviceItem device)
		{
			if (contextItem != null)
			{
				var renderings = contextItem.Visualization.GetRenderings(device, true);
				return renderings;
			}
			return new RenderingReference[0];
		}
	}
}
