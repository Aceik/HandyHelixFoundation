namespace Site.Foundation.Analytics.Processor
{
	using System;
	using System.Linq;
	using Sitecore.Analytics;
	using Sitecore.Analytics.Data.Items;
	using Sitecore.Analytics.Model.Entities;
	using Sitecore.Analytics.Outcome.Extensions;
	using Sitecore.Analytics.Tracking;
	using Sitecore.Configuration;
	using Sitecore.Data;
	using Sitecore.Diagnostics;
	using Sitecore.FXM.Pipelines.Tracking.RegisterPageEvent;
	using Sitecore.FXM.Tracking;
	using Site.Foundation.Analytics.Model;

	public class RegisterPageEventProcessor : Sitecore.FXM.Pipelines.Tracking.RegisterPageEvent.RegisterPageEventProcessor
	{
		public new void Process(RegisterPageEventArgs args)
		{
			Assert.ArgumentNotNull(args, "args");
			Assert.IsNotNull(args.PageEventItem, "No item has been found corresponding to the page event.");
			Assert.IsNotNull(args.CurrentPage, "The current page is not tracked in the current session.  No events can be triggered.");
			switch (args.EventParameters.EventType)
			{
				case PageEventType.Goal:
				case PageEventType.Event:
					this.TriggerPageEvent(args);
					break;
				case PageEventType.Campaign:
					this.TriggerCampaign(args);
					break;
				case PageEventType.Outcome:
					this.TriggerOutcome(args);
					break;
				case PageEventType.Element:
					this.TriggerElement(args);
					break;
			}
		}

		protected override void TriggerPageEvent(RegisterPageEventArgs args)
		{
			// let the base method do it's stuff and register the page event            
			base.TriggerPageEvent(args);

			// here we are trying to update the current contact data
			if (Tracker.Current != null && Tracker.Current.Contact != null)
			{
				if (string.Equals(args.PageEventItem.Name, "updatePersonalInfo", StringComparison.OrdinalIgnoreCase))
				{
					this.UpdatePersonalInfo(Tracker.Current.Contact, args);
				}
				else if (string.Equals(args.PageEventItem.Name, "identifyContact", StringComparison.OrdinalIgnoreCase))
				{
					this.IdentifyContact(args);
				}
				else if (string.Equals(args.PageEventItem.Name, "updateEmail", StringComparison.OrdinalIgnoreCase))
				{
					this.UpdateEmail(Tracker.Current.Contact, args);
				}
				else if (string.Equals(args.PageEventItem.Name, "updateGoogleCid", StringComparison.OrdinalIgnoreCase))
				{
					this.UpdateGoogleCid(Tracker.Current.Contact, args);
				}
				else if (string.Equals(args.PageEventItem.Name, "Newsletter Signup", StringComparison.OrdinalIgnoreCase) || string.Equals(args.PageEventItem.Name, "Submit an Enquiry", StringComparison.OrdinalIgnoreCase))
				{
					this.RegisterIdentificationOutcome(Tracker.Current.Contact, args);
				}
			}
			else
			{
				Log.Info("POC: cannot find the contact for the current interaction", this);
			}
		}

		private void RegisterIdentificationOutcome(Contact contact, RegisterPageEventArgs args)
		{
			ID id = Sitecore.Data.ID.NewID;
			ID interactionId = Sitecore.Data.ID.NewID;
			ID contactId = Sitecore.Data.ID.NewID;

			// definition item for Sales Lead
			var definitionId = FoundationAnalyticsConstants.Outcomes.MarketingLeadOutcomeId;

			var outcome = new Sitecore.Analytics.Outcome.Model.ContactOutcome(id, definitionId, contactId)
			{
				DateTime = DateTime.UtcNow.Date,
				MonetaryValue = 0,
				InteractionId = interactionId
			};

			Tracker.Current.RegisterContactOutcome(outcome);
		}

		private void UpdateGoogleCid(Contact contact, RegisterPageEventArgs args)
		{
			try
			{
				IGoogleClientIdentifier googleContactIdentifier = contact.GetFacet<IGoogleClientIdentifier>("Cid");
				googleContactIdentifier.Cid = args.EventParameters.Data;
				Tracker.Current.Session.Identify(args.EventParameters.Data);

				CampaignItem item = new CampaignItem(Sitecore.Context.Database.GetItem(ID.NewID));
				Sitecore.Analytics.Tracker.Current.CurrentPage.TriggerCampaign(item);
			}
			catch (Exception ex)
			{
				Sitecore.Diagnostics.Log.Error("Could not indentify user and enrol in checkout campaign", ex);
			}
		}

		private void UpdateEmail(Contact contact, RegisterPageEventArgs args)
		{
			var contactEmailAddresses = contact.GetFacet<IContactEmailAddresses>("Emails");
			if (contactEmailAddresses != null &&
				string.Equals(args.EventParameters.DataKey, "email", StringComparison.OrdinalIgnoreCase))
			{
				string emailData = args.EventParameters.Data;
				IEmailAddress email;
				if (!contactEmailAddresses.Entries.Contains("Work Email"))
				{
					email = contactEmailAddresses.Entries.Create("Work Email");
				}
				else
				{
					email = contactEmailAddresses.Entries["Work Email"];
				}
				email.SmtpAddress = emailData;
				contactEmailAddresses.Preferred = "Work Email";
			}
		}

		private void UpdatePersonalInfo(Contact contact, RegisterPageEventArgs args)
		{
			var personalInfo = contact.GetFacet<IContactPersonalInfo>("Personal");
			if (personalInfo != null && args.EventParameters.Extras.Any())
			{
				var values = args.EventParameters.Extras;

				if (values.ContainsKey("FirstName"))
				{
					personalInfo.FirstName = values["FirstName"];
				}
				if (values.ContainsKey("Surname"))
				{
					personalInfo.Surname = values["Surname"];
				}
				if (values.ContainsKey("Gender"))
				{
					personalInfo.Gender = values["Gender"];
				}
			}

			var addressInfo = contact.GetFacet<IContactAddresses>("Addresses");
			if (addressInfo != null && args.EventParameters.Extras.Any())
			{
				var values = args.EventParameters.Extras;

				IAddress address;
				if (!addressInfo.Entries.Contains("Home"))
				{
					address = addressInfo.Entries.Create("Home");
				}
				else
				{
					address = addressInfo.Entries["Home"];
				}

				addressInfo.Preferred = "Home";
				if (values.ContainsKey("Country"))
				{
					address.Country = values["Country"];
				}
				if (values.ContainsKey("Postalcode"))
				{
					address.PostalCode = values["Postalcode"];
				}
			}
		}

		private void IdentifyContact(RegisterPageEventArgs args)
		{
			if (string.Equals(args.EventParameters.DataKey, "id", StringComparison.OrdinalIgnoreCase) &&
				!string.IsNullOrWhiteSpace(args.EventParameters.Data))
			{
				Tracker.Current.Session.Identify(args.EventParameters.Data);
			}
		}
	}
}
