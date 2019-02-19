namespace Site.Foundation.Analytics.Processor
{
	using Sitecore.Diagnostics;
	using Sitecore.FXM.Pipelines.Tracking.RegisterPageEvent;
	using Sitecore.FXM.Pipelines.Tracking.TriggerPageEvent;
	using Sitecore.Pipelines;

	public class SiteRunRegisterPageEventProcessor : ITriggerPageEventProcessor, ITriggerPageEventProcessor<ITriggerPageEventArgs>
	{
		private readonly CorePipeline pipeline;

		public SiteRunRegisterPageEventProcessor() : this(new Sitecore.Abstractions.CorePipelineWrapper())
		{
		}

		public SiteRunRegisterPageEventProcessor(Sitecore.Abstractions.ICorePipeline corePipeline)
		{
			//pipeline = corePipeline as CorePipeline;
		}

		public void Process(ITriggerPageEventArgs args)
		{
			Assert.ArgumentNotNull(args, "args");
			Assert.IsNotNull(args.CurrentPageVisit, "The current page is not tracked in the current session.  No events can be triggered.");
			RegisterPageEventArgs registerPageEventArgs = new RegisterPageEventArgs(args.CurrentPageVisit, args.EventParameters);
			CorePipeline.Run("tracking.registerpageevent", registerPageEventArgs, "FXM");
			if (registerPageEventArgs.PipelineResult.Code != 0)
			{
				args.AbortAndFailPipeline(registerPageEventArgs.PipelineResult.Message, registerPageEventArgs.PipelineResult.Code);
			}
		}
	}
}
