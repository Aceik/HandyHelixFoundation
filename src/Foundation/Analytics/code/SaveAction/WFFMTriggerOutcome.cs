using Sitecore.Analytics;
using Sitecore.Analytics.Outcome.Extensions;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.WFFM.Abstractions.Actions;
using Sitecore.WFFM.Abstractions.Analytics;
using Sitecore.WFFM.Abstractions.Dependencies;
using Sitecore.WFFM.Actions.Base;
using Sitecore.WFFM.Analytics.Providers;
using System;
using System.Collections.Generic;
using System.Web;

namespace Site.Foundation.Analytics.SaveAction
{
	public class WFFMTriggerOutcome : WffmSaveAction
	{
		public override void Execute(ID formId, AdaptedResultList adaptedFields, ActionCallContext actionCallContext = null, params object[] data)
		{
			try
			{
				if (Tracker.Current == null)
					return;

				ID id = Sitecore.Data.ID.NewID;
				ID interactionId = Sitecore.Data.ID.NewID;
				ID contactId = Sitecore.Data.ID.NewID;

				// definition item for Marketing Lead
				var definitionId = Site.Foundation.Analytics.FoundationAnalyticsConstants.Outcomes.MarketingLeadOutcomeId;

				var outcome = new Sitecore.Analytics.Outcome.Model.ContactOutcome(id, definitionId, contactId)
				{
					DateTime = DateTime.UtcNow.Date,
					MonetaryValue = 0,
					InteractionId = interactionId
				};

				Tracker.Current.RegisterContactOutcome(outcome);

				Log.Audit("WFFMTriggerOutcome : Outcome recorded ", this);
			}
			catch (Exception e)
			{
				Log.Error("WFFMTriggerOutcome : Exception occured while triggering outcome " + e.Message, this);
			}
		}
		
	}
}
