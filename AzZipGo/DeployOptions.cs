using Mono.Options;

namespace AzZipGo;

public abstract class DeployOptions : Options
{
    public DeployOptions()
    {
        Command.Options.Add("s|subscription=", "The subscription ID.", s => Subscription = s);
        Command.Options.Add("g|resource-group=", "The resource group of the website.", s => ResourceGroup = s);
        Command.Options.Add("d|directory=", "The path to the directory to deploy.", s => Directory = s);
        Command.Options.Add("site=", "The site name to deploy to.", s => Site = s);
        Command.Options.Add("target-slot=", "The slot name to deploy to. Use `production` to deploy to the specified website directly.", s => TargetSlot = s);
        Command.Options.Add("run-from-package", "Set or remove app setting WEBSITE_RUN_FROM_PACKAGE for the target or temp slot. This will enable or disable the Run From Package feature. Default = false", s => RunFromPackage = (s != null));
    }

    public string Subscription { get; set; }
    public string ResourceGroup { get; set; }
    public string Directory { get; set; }
    public string Site { get; set; }
    public string TargetSlot { get; set; }
    public bool RunFromPackage { get; set; }
}
