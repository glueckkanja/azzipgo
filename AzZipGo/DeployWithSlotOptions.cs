using CommandLine;

namespace AzZipGo
{
    [Verb("deploy-with-slot", HelpText = "Deploy Site using ZipDeploy and a newly created slot.")]
    public class DeployWithSlotOptions : DeployOptions
    {
        [Option("cleanup-after-success", Default = false)]
        public bool CleanupAfterSuccess { get; set; }
    }
}