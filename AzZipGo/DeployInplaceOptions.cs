using CommandLine;

namespace AzZipGo
{
    [Verb("deploy-inplace", HelpText = "Deploy Site using ZipDeploy and run the deployment in-place without a temporary slot and auto-swap.")]
    public class DeployInplaceOptions : DeployOptions
    {
    }
}