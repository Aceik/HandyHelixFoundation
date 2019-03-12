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

        public struct Events
        {
            public static string UpdateGoogleCidLocal => "UpdateGoogleCidLocal";
            public static string UpdateGoogleCidRemote => "UpdateGoogleCidRemote";
        }
    }
}
