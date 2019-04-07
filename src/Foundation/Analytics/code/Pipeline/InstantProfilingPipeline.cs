using Sitecore.Analytics;
using Sitecore.Analytics.Data;
using Sitecore.Analytics.Tracking;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml;

namespace Site.Foundation.Analytics.Pipeline
{
    using Sitecore.Pipelines;

    /// <summary>
    /// This pipeline was based on the answer from: https://sitecore.stackexchange.com/questions/6801/programmatically-trigger-a-pattern-card-profile-card-in-a-selected-content-in-si
    /// Currently works in Sitecore 8. May require some refinement for Sitecore 9.
    /// </summary>
    public class InstantProfilingPipeline
    {
        protected const string ProfileOneParameter = "pr";
        protected const string PatternOneParameter = "pa";

        protected const string ProfileTwoParameter = "pr2";
        protected const string PatternTwoParameter = "pa2";

        protected const string ProfileThreeParameter = "pr3";
        protected const string PatternThreeParameter = "pa3";

        public virtual void Process(PipelineArgs args)
        {
            try
            {
                DetectUrlParameterAndMatch();
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException) return;
                Log.Error("PredefinePersonaPipeline - Exception occured", ex);
            }
        }

        /// <summary>
        /// Method will look in the URL from the Instant Personalisation parameter.
        /// </summary>
        private void DetectUrlParameterAndMatch()
        {
            // Pattern 1: does it exist. If not exist quickly.
            if (HttpContext.Current == null || HttpContext.Current?.Request?[ProfileOneParameter] == null || HttpContext.Current?.Request?[PatternOneParameter] == null)
                return;

            // Pattern 1: fire off a match
            this.BoostUserPattern(Tracker.Current.Session, HttpContext.Current.Request[ProfileOneParameter], HttpContext.Current.Request[PatternOneParameter]);

            // Pattern 2: Does a second profile exist in the URL. If not wrap it and return;
            if (HttpContext.Current?.Request?[ProfileTwoParameter] == null || HttpContext.Current?.Request?[PatternTwoParameter] == null)
            {
                Tracker.Current.Interaction.AcceptModifications();
                return;
            }

            // Pattern 2: fire off a match
            this.BoostUserPattern(Tracker.Current.Session, HttpContext.Current.Request[ProfileTwoParameter], HttpContext.Current.Request[PatternTwoParameter]);

            // Pattern 3: Does a third profile exist in the URL. If not wrap it and return;
            if (HttpContext.Current?.Request?[ProfileThreeParameter] == null || HttpContext.Current?.Request?[PatternThreeParameter] == null)
            {
                Tracker.Current.Interaction.AcceptModifications();
                return;
            }

            // Pattern 3: fire off a match
            this.BoostUserPattern(Tracker.Current.Session, HttpContext.Current.Request[ProfileThreeParameter], HttpContext.Current.Request[PatternThreeParameter]);
            Tracker.Current.Interaction.AcceptModifications();
        }

        /// <summary>
        /// Locate a profile and a pattern and give the user the score associated with the pattern card. This will all personsliation rules to be run after this pipeline. 
        /// </summary>
        /// <param name="session">The current tracking session from XDB.</param>
        /// <param name="profileName">The name of the profile you want to match within</param>
        /// <param name="patternName">The name of the pattern card that will be recorded against the user</param>
        protected void BoostUserPattern(Session session, string profileName, string patternName)
        {
            var profiles = Sitecore.Context.Database.GetItem(FoundationAnalyticsConstants.Profiles.ProfilesParent);
            if (!profiles.Children.Any())
                return;

            var matchingProfile = profiles.Children.FirstOrDefault(x => x.Name.ToLower() == profileName.ToLower());
            if (matchingProfile == null)
                return;

            // If we find that a profile is already set for this user abort
            if (session.Contact.BehaviorProfiles.Profiles.Any())
            {
                if (session.Contact.BehaviorProfiles.Profiles.Any(x => matchingProfile.ID == x.Id && x.NumberOfTimesScored > 0))
                    return;
            }

            var patternCardParent = matchingProfile.Children.FirstOrDefault(x => x.TemplateID == FoundationAnalyticsConstants.Profiles.PatternCardParentTemplate);
            if (patternCardParent == null || !patternCardParent.Children.Any())
                return;

            var patternCard = patternCardParent.Children.FirstOrDefault(x => this.FilterName(x.Name) == this.FilterName(patternName));// Get the specific pattern you want from the list
            if (patternCard == null)
                return;

            // Looking up the profile from the interaction. This is a reference to the profile for this individual user.
            // It is used in the methods below in BoostUserPatternViaItem
            var profile = session.Interaction.Profiles[patternCard.Parent.Parent.Name];

            this.BoostUserPatternViaItem(session, patternCard, profile);
        }

        /// <summary>
        /// Apply some filters to the names passed in.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string FilterName(string name)
        {
            return name.Trim().ToLower().Replace(" ", string.Empty).Replace("-", string.Empty);
        }

        /// <summary>
        /// Apply the scores from the pattern match, to the profile in the interaction session.
        /// </summary>
        /// <param name="session">The XDB session</param>
        /// <param name="patternCard">Pattern card item within sitecore</param>
        /// <param name="profile">Reference to the profile from withing the user interaction object. Changes to this profile will happen just for this user.</param>
        public void BoostUserPatternViaItem(Session session, Item patternCard, Profile profile)
        {
            if (patternCard != null && !patternCard.Name.Equals(profile.PatternLabel))
            {
                Sitecore.Data.Fields.XmlField xmlData = patternCard.Fields["Pattern"];
                XmlDocument xmlDoc = xmlData.Xml;

                XmlNodeList parentNode = xmlDoc.GetElementsByTagName("key");
                var scores = new Dictionary<string, float>();

                foreach (XmlNode childrenNode in parentNode)
                {
                    if (childrenNode.Attributes != null)
                    {
                        var score = float.Parse(childrenNode.Attributes["value"].Value);
                        scores.Add(childrenNode.Attributes["name"].Value, score);
                    }
                }

                // Set a score value here
                scores[patternCard.Name] = 5;

                profile.Score(scores);

                profile.PatternId = patternCard.ID.ToGuid();
                profile.PatternLabel = patternCard.Name;

                UpdateBehaviorProfile(session);
            }
        }

        /// <summary>
        /// Update the contact with the updated profiles in the interaction session. 
        /// </summary>
        /// <param name="session"></param>
        private static void UpdateBehaviorProfile(Session session)
        {
            var profileConverterBase = BehaviorProfileConverterBase.Create();

            if (session?.Contact == null || Tracker.Current.Interaction == null)
            {
                return;
            }

            foreach (var profileName in session.Interaction.Profiles.GetProfileNames())
            {
                var profile = session.Interaction.Profiles[profileName];
                if (!IgnoreInteractionProfile(profile))
                {
                    var matchedBehaviorProfile = profileConverterBase.Convert(profile);
                    session.Contact.BehaviorProfiles.Add(matchedBehaviorProfile.Id, matchedBehaviorProfile);
                }
            }
        }

        private static bool IgnoreInteractionProfile(Profile profile)
        {
            Assert.ArgumentNotNull(profile, "profile");
            return false;
        }
    }
}
