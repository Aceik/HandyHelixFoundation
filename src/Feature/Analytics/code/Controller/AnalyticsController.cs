// Derivative work from:  https://jabaremitchell.wordpress.com/2016/09/06/trigger-goals-in-sitecore-8-mvc-using-jquery-post-and-glassmapper/
// Credit goes to JABARE LANARD for his original example. 
// Many Thanks.

using System;
using System.Linq;
using System.Web.Mvc;

using Sitecore.Analytics;
using Sitecore.Analytics.Data.Items;
using Sitecore.Mvc.Controllers;

namespace Site.Feature.Analytics.Controller
{
	using Sitecore.Analytics.Model.Entities;

	[AllowCrossSiteJson]
	public class AnalyticsController : SitecoreController
	{
		[HttpPost]
		public ActionResult TriggerGoal(string goal)
		{
			if (!Tracker.IsActive || Tracker.Current == null)
			{
				Tracker.StartTracking();
			}

			if (Tracker.Current == null)
			{
				return Json(new { Success = false, Error = "Can't activate tracker" });
			}

			if (string.IsNullOrEmpty(goal))
			{
				return Json(new { Success = false, Error = "Goal not set" });
			}

			var goalRootItem = Sitecore.Context.Database.GetItem(AnalyticsConstants.Goals.DefaultGoalLocation);
			var goalItem = goalRootItem.Axes.GetDescendants().FirstOrDefault(x => x.Name.Equals(goal, StringComparison.InvariantCultureIgnoreCase));

			if (goalItem == null)
			{
				return Json(new { Success = false, Error = "Goal not found" });
			}

			var page = Tracker.Current.Session.Interaction.PreviousPage;
			if (page == null)
			{
				return Json(new { Success = false, Error = "Page is null" });
			}

			var registerTheGoal = new PageEventItem(goalItem);
			var eventData = page.Register(registerTheGoal);
			eventData.Data = goalItem["Description"];
			eventData.ItemId = goalItem.ID.Guid;
			eventData.DataKey = goalItem.Paths.Path;
			Tracker.Current.Interaction.AcceptModifications();

			Tracker.Current.CurrentPage.Cancel();

			return Json(new { Success = true });
		}

		[HttpPost]
		public ActionResult TriggerEvent(string eventName, string data, bool shouldUpdate = false)
		{
			if (!Tracker.IsActive || Tracker.Current == null)
			{
				Tracker.StartTracking();
			}

			if (Tracker.Current == null)
			{
				return Json(new { Success = false, Error = "Can't activate tracker" });
			}

			if (string.IsNullOrEmpty(eventName))
			{
				return Json(new { Success = false, Error = "Event not set" });
			}

			var eventsRoot = Sitecore.Context.Database.GetItem(AnalyticsConstants.Events.EventLocation);
			var eventItem = eventsRoot.Axes.GetDescendants().FirstOrDefault(x => x.Name.Equals(eventName, StringComparison.InvariantCultureIgnoreCase));

			if (eventItem == null)
			{
				return Json(new { Success = false, Error = "Event not found" });
			}

			var page = Tracker.Current.Session.Interaction.PreviousPage;
			if (page == null)
			{
				return Json(new { Success = false, Error = "Page is null" });
			}

			var registerTheEvent = new PageEventItem(eventItem);
			var eventData = page.Register(registerTheEvent);
			eventData.Data = data;
			eventData.ItemId = eventItem.ID.Guid;
			eventData.DataKey = eventItem.Paths.Path;
			Tracker.Current.Interaction.AcceptModifications();

			Tracker.Current.CurrentPage.Cancel();

			CheckForGoogleClientId(eventName, data, shouldUpdate);

			return Json(new { Success = true });
		}

		private void CheckForGoogleClientId(string eventName, string data, bool shouldUpdate)
		{
			try
			{
				if (eventName == FeatureAnalyticsConstants.Events.UpdateGoogleCidLocal && !string.IsNullOrWhiteSpace(data))
				{
					IGoogleClientIdentifier googleContactIdentifier = Tracker.Current.Contact.GetFacet<IGoogleClientIdentifier>("Cid");
					if (string.IsNullOrWhiteSpace(googleContactIdentifier.Cid))
					{
						googleContactIdentifier.Cid = data;
						Tracker.Current.Session.Identify(data);
					}

					// Only perform the update if requested and the current recorded CID is empty or matches the cid being passed in.
					var doesGoogleIdentifierMatch = string.IsNullOrWhiteSpace(googleContactIdentifier.Cid) || googleContactIdentifier.Cid == data;

					if (shouldUpdate && doesGoogleIdentifierMatch)
					{
						SetPersonalDetails(data);
					}
				}
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("Could not indentify user and enroll in checkout campaign", ex);
			}
		}

		private void SetPersonalDetails(string data)
		{
			var personalInfo = Tracker.Current.Contact.GetFacet<IContactPersonalInfo>("Personal");
			if (string.IsNullOrWhiteSpace(personalInfo.FirstName) && string.IsNullOrWhiteSpace(personalInfo.Surname))
			{
				if (data.Contains("."))
				{
					var cidTokens = data.Split('.');
					personalInfo.FirstName = cidTokens[0];
					if (cidTokens.Length > 1)
						personalInfo.Surname = data.Split('.')[1];
				}

			}
		}
	}
}
