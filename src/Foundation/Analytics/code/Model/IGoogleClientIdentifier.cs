namespace Site.Foundation.Analytics.Model
{
	using Sitecore.Analytics.Model.Framework;

	public interface IGoogleClientIdentifier : IFacet, IElement
	{
		string Cid { get; set; }
	}
}
