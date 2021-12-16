namespace AzZipGo;

public class DeployInplaceOptions : DeployOptions
{
    public DeployInplaceOptions()
    {
    }

    public override string CommandName => "deploy-in-place";
    public override string CommandHelp => "Deploy Site using ZipDeploy and run the deployment in-place without a temporary slot and auto-swap.";
}
