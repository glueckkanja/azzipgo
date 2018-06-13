using Mono.Options;

namespace AzZipGo
{
    public class DeployWithSlotOptions : DeployOptions
    {
        public DeployWithSlotOptions()
        {
            Command.Options.Add("cleanup-after-success", "Delete temporary slot after deployment. This will add a wait period of 2 minutes. Default = false", s => CleanupAfterSuccess = (s != null));
            Command.Options.Add("stop-webjobs", "Set sticky app setting WEBJOBS_STOPPED=1 for the new temporary slot. Default = true", s => StopWebjobs = (s != null));
        }

        public override string CommandName => "deploy-with-slot";
        public override string CommandHelp => "Deploy Site using ZipDeploy and a newly created slot.";

        public bool CleanupAfterSuccess { get; set; }
        public bool StopWebjobs { get; set; } = true;
    }
}