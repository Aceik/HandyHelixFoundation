using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Analytics.Data;

// https://sitecore.stackexchange.com/questions/4527/personalize-a-component-using-outcomes/8744
namespace Site.Foundation.Analytics.Condition
{
	// Sitecore.Analytics.Rules.Conditions.GoalWasTriggeredDuringPastOrCurrentInteractionCondition<T>
	using Sitecore.Analytics;
	using Sitecore.Analytics.Core;
	using Sitecore.Analytics.Model;
	using Sitecore.Analytics.Rules.Conditions;
	using Sitecore.Analytics.Tracking;
	using Sitecore.Diagnostics;
	using Sitecore.Rules;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class GoalWasTriggeredDuringPastInteractionCondition<T> : Site.Foundation.Analytics.Condition.HasEventOccurredCondition<T> where T : RuleContext
	{
		private Guid? goalGuid;

		private bool goalGuidInitialized;

		public string GoalId
		{
			get;
			set;
		}

		private Guid? GoalGuid
		{
			get
			{
				if (goalGuidInitialized)
				{
					return goalGuid;
				}
				try
				{
					goalGuid = new Guid(GoalId);
				}
				catch
				{
					Log.Warn($"Could not convert value to guid: {GoalId}", GetType());
				}
				goalGuidInitialized = true;
				return goalGuid;
			}
		}

		public GoalWasTriggeredDuringPastInteractionCondition()
			: base(filterByCustomData: false)
		{
		}

		protected GoalWasTriggeredDuringPastInteractionCondition(bool filterByCustomData)
			: base(filterByCustomData)
		{
		}

		protected override bool Execute(T ruleContext)
		{
			Assert.ArgumentNotNull(ruleContext, "ruleContext");
			Assert.IsNotNull(Tracker.Current, "Tracker.Current is not initialized");
			Assert.IsNotNull(Tracker.Current.Session, "Tracker.Current.Session is not initialized");

			if (Tracker.Current.Session.Interaction == null)
			{
				return this.AlternativeExecution(ruleContext);
			}

			Assert.IsNotNull(Tracker.Current.Session.Interaction, "Tracker.Current.Session.Interaction is not initialized");
			if (!GoalGuid.HasValue)
			{
				return false;
			}
			if (Tracker.Current.Session.Interaction != null && HasEventOccurredInInteraction(Tracker.Current.Session.Interaction))
			{
				return true;
			}
			Assert.IsNotNull(Tracker.Current.Contact, "Tracker.Current.Contact is not initialized");
			try
			{
				KeyBehaviorCache keyBehaviorCache = Tracker.Current.Contact.GetKeyBehaviorCache();
				return FilterKeyBehaviorCacheEntries(keyBehaviorCache).Any(delegate(KeyBehaviorCacheEntry entry)
				{
					Guid id = entry.Id;
					Guid? b = GoalGuid;
					return id == b;
				});
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Debug("Issue looking up goal history", ex);
				return this.AlternativeExecution(ruleContext);
			}

			return false;
		}

		private bool AlternativeExecution(T ruleContext)
		{
			try
			{
				if (!this.GoalGuid.HasValue)
				{
					return false;
				}

				var behaviourCache = this.GetContactBehaviourCache(Tracker.Current.Contact);
				if (behaviourCache?.Goals != null && behaviourCache.Goals.Any(x => x.Id == this.GoalGuid.Value))
					return true;
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Debug("Issue looking up alternative goal history", ex);
			}

			return false;
		}

		public List<IInteractionData> GetContactHistory(Contact contact)
		{
			var contactManager = Sitecore.Configuration.Factory.CreateObject("tracking/contactManager", true) as ContactManager;
			if (contactManager != null)
			{
				contact.LoadKeyBehaviorCache();
				var historicalDataResults = contact.LoadHistorycalData(1).ToList();
				return historicalDataResults;
			}

			return null;
		}

		public KeyBehaviorCache GetContactBehaviourCache(Contact contact)
		{
			var contactManager = Sitecore.Configuration.Factory.CreateObject("tracking/contactManager", true) as ContactManager;
			if (contactManager != null)
			{
				contact.LoadKeyBehaviorCache();
				var behaviorCacheResults = contact.GetKeyBehaviorCache();
				return behaviorCacheResults;
			}

			return null;
		}

		protected override IEnumerable<KeyBehaviorCacheEntry> GetKeyBehaviorCacheEntries(KeyBehaviorCache keyBehaviorCache)
		{
			Assert.ArgumentNotNull(keyBehaviorCache, "keyBehaviorCache");
			return keyBehaviorCache.Goals;
		}

		protected override bool HasEventOccurredInInteraction(IInteractionData interaction)
		{
			Assert.ArgumentNotNull(interaction, "interaction");
			Assert.IsNotNull(interaction.Pages, "interaction.Pages is not initialized.");
			return interaction.Pages.SelectMany((Page page) => page.PageEvents).Any(delegate (PageEventData pageEvent)
			{
				if (pageEvent.IsGoal)
				{
					Guid pageEventDefinitionId = pageEvent.PageEventDefinitionId;
					Guid? b = GoalGuid;
					return pageEventDefinitionId == b;
				}
				return false;
			});
		}
	}
}
