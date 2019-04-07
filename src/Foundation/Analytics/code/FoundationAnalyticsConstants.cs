namespace Site.Foundation.Analytics
{
	using Sitecore.Data;

	public struct FoundationAnalyticsConstants
	{
		public struct Campaigns
		{
		}

        public struct Profiles
        {
            public static ID ProfilesParent => new ID("{12BD7E35-437B-449C-B931-23CFA12C03D8}");
            public static ID PatternCardParentTemplate => new ID("{0771B0A2-5BCF-4F87-91DE-13474618B6BF}");
        }

        public struct Outcomes
		{
			public static ID MarketingLeadOutcomeId => new ID("{52054874-4767-47DC-8099-8C08BFA307AA}");
		}
    }
}
