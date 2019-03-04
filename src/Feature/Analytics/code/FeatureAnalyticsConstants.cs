namespace Site.Feature.Analytics
{
	using Sitecore.Data;

	public struct FeatureAnalyticsConstants
	{
        public struct Cookies
        {
            public static string GlobalMultiSiteCookieName => "GlobalCidCookie";
            public static string LocalMultiSiteCookieName => "LocalCidCookie";
        }
    }
}
