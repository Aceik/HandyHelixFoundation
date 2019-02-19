using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Site.Foundation.Analytics.Condition
{
	using Sitecore.Analytics.Tracking;
	using Sitecore.Diagnostics;
	using Sitecore.Rules;
	using Sitecore.Rules.Conditions;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public abstract class HasEventOccurredCondition<T> : WhenCondition<T> where T : RuleContext
	{
		private readonly bool filterByCustomData;

		public string CustomData
		{
			get;
			set;
		}

		public string CustomDataOperatorId
		{
			get;
			set;
		}

		public int NumberOfElapsedDays
		{
			get;
			set;
		}

		public string NumberOfElapsedDaysOperatorId
		{
			get;
			set;
		}

		public int NumberOfPastInteractions
		{
			get;
			set;
		}

		public string NumberOfPastInteractionsOperatorId
		{
			get;
			set;
		}

		protected HasEventOccurredCondition(bool filterByCustomData)
		{
			this.filterByCustomData = filterByCustomData;
		}

		protected virtual IEnumerable<KeyBehaviorCacheEntry> FilterKeyBehaviorCacheEntries(KeyBehaviorCache keyBehaviorCache)
		{
			Assert.ArgumentNotNull(keyBehaviorCache, "keyBehaviorCache");
			IEnumerable<KeyBehaviorCacheEntry> keyBehaviorCacheEntries = keyBehaviorCache.Campaigns.Concat(keyBehaviorCache.Channels).Concat(keyBehaviorCache.CustomValues).Concat(keyBehaviorCache.Goals)
				.Concat(keyBehaviorCache.Outcomes)
				.Concat(keyBehaviorCache.PageEvents)
				.Concat(keyBehaviorCache.Venues);
			IEnumerable<KeyBehaviorCacheEntry> enumerable = FilterKeyBehaviorCacheEntriesByInteractionConditions(keyBehaviorCacheEntries);
			if (filterByCustomData)
			{
				if (CustomData == null)
				{
					Log.Warn("CustomData can not be null", GetType());
					return Enumerable.Empty<KeyBehaviorCacheEntry>();
				}
				enumerable = enumerable.Where(delegate (KeyBehaviorCacheEntry entry)
				{
					if (entry.Data != null)
					{
						return ConditionsUtility.CompareStrings(entry.Data, CustomData, CustomDataOperatorId);
					}
					return false;
				});
			}
			return Assert.ResultNotNull(GetKeyBehaviorCacheEntries(keyBehaviorCache).Intersect(enumerable, new KeyBehaviorCacheEntry.KeyBehaviorCacheEntryEqualityComparer()));
		}

		protected virtual IEnumerable<KeyBehaviorCacheEntry> FilterKeyBehaviorCacheEntriesByInteractionConditions(IEnumerable<KeyBehaviorCacheEntry> keyBehaviorCacheEntries)
		{
			Assert.ArgumentNotNull(keyBehaviorCacheEntries, "keyBehaviorCacheEntries");
			if (ConditionsUtility.GetInt32Comparer(NumberOfElapsedDaysOperatorId) == null)
			{
				return Enumerable.Empty<KeyBehaviorCacheEntry>();
			}

			Func<int, int, bool> numberOfElapsedDaysComparer = ConditionsUtility.GetInt32Comparer(NumberOfElapsedDaysOperatorId);

			if (numberOfElapsedDaysComparer == null)
				return Enumerable.Empty<KeyBehaviorCacheEntry>();

			Func<int, int, bool> numberOfPastInteractionsComparer = ConditionsUtility.GetInt32Comparer(NumberOfPastInteractionsOperatorId);
			if (numberOfPastInteractionsComparer == null)
			{
				return Enumerable.Empty<KeyBehaviorCacheEntry>();
			}
			return Assert.ResultNotNull((from entry in keyBehaviorCacheEntries
			group entry by new
			{
				entry.InteractionId,
				entry.InteractionStartDateTime
			} into entries
			orderby entries.Key.InteractionStartDateTime descending
			select entries).Where((entries, i) =>
			{
				if (numberOfPastInteractionsComparer((DateTime.UtcNow - entries.Key.InteractionStartDateTime).Days, NumberOfElapsedDays))
				{
					return numberOfPastInteractionsComparer(i + 2, NumberOfPastInteractions);
				}
				return false;
			}).SelectMany(entries => entries));
		}

		protected abstract IEnumerable<KeyBehaviorCacheEntry> GetKeyBehaviorCacheEntries(KeyBehaviorCache keyBehaviorCache);

		protected abstract bool HasEventOccurredInInteraction(IInteractionData interaction);
	}
}
