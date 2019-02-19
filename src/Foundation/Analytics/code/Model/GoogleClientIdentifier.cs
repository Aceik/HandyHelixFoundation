using Sitecore.Analytics.Model.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Site.Foundation.Analytics.Model
{
	[Serializable]
	public class GoogleClientIdentifier : Element, IGoogleClientIdentifier
	{
		private const string GoogleCid = "cid";
		public string Cid
		{
			get
			{
				return this.GetAttribute<string>(GoogleCid);
			}
			set
			{
				this.SetAttribute(GoogleCid, value);
			}
		}

		public GoogleClientIdentifier()
		{
			this.EnsureAttribute<string>(GoogleCid);
		}
	}
}
