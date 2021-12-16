using Microsoft.Azure.Management.ResourceManager.Fluent;
using Mono.Options;
using System;

namespace AzZipGo;

public abstract class Options
{
    public Options()
    {
        Command = new Command(CommandName, CommandHelp)
        {
            Options = new OptionSet(),
            Run = (args) => IsActive = true,
        };

        Command.Options.Add("u|user=", "Service principal ID. Create in Azure using `az ad sp create-for-rbac`.", s => User = s);
        Command.Options.Add("p|password=", "Service principal password.", s => Password = s);
        Command.Options.Add("t|tenant=", "The tenant ID or name.", s => Tenant = s);
        Command.Options.Add("environment:", "The Azure environment. One of: global (default), germany, china, usgov", (AzureEnvironmentOption s) => Environment = s);
    }

    public Command Command { get; }
    public bool IsActive { get; private set; }

    public abstract string CommandName { get; }
    public abstract string CommandHelp { get; }

    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string Tenant { get; set; } = "";

    public AzureEnvironmentOption Environment { get; set; } = AzureEnvironmentOption.Global;

    public AzureEnvironment AzureEnvironment
    {
        get
        {
            switch (Environment)
            {
                case AzureEnvironmentOption.Global: return AzureEnvironment.AzureGlobalCloud;
                case AzureEnvironmentOption.Germany: return AzureEnvironment.AzureGermanCloud;
                case AzureEnvironmentOption.China: return AzureEnvironment.AzureChinaCloud;
                case AzureEnvironmentOption.UsGov: return AzureEnvironment.AzureUSGovernment;
                default: throw new ArgumentException("Invalid selection for Environment");
            }
        }
    }

    public enum AzureEnvironmentOption
    {
        Global,
        Germany,
        China,
        UsGov,
    }
}
