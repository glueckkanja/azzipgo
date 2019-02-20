using Mono.Options;

namespace AzZipGo
{
    public class DeployInplaceOptions : DeployOptions
    {
        public DeployInplaceOptions()
        {
            Command.Options.Add("run-from-package", "Set or remove sticky app setting WEBSITE_RUN_FROM_PACKAGE for the target slot. This will enable or disable the Run From Package feature. Default = false", s => RunFromPackage = (s != null));
        }

        public override string CommandName => "deploy-in-place";
        public override string CommandHelp => "Deploy Site using ZipDeploy and run the deployment in-place without a temporary slot and auto-swap.";

        public bool RunFromPackage { get; set; }
    }
}