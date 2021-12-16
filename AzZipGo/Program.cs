using Mono.Options;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace AzZipGo;

public class Program
{
    public static string? MyVersion => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    private static async Task<int> Main(string[] args)
    {
        var deployWithSlotOptions = new DeployWithSlotOptions();
        var deployInplaceOptions = new DeployInplaceOptions();

        var suite = new CommandSet("azzipgo") {
                $"Azure Zip'n'Go {MyVersion ?? "(unknown version)"}",
                "",
                "Usage: azzipgo COMMAND [OPTIONS]+",
                deployWithSlotOptions.Command,
                deployInplaceOptions.Command,
            };

        var code = suite.Run(args);

        if (code != 0)
        {
            return code;
        }

        if (deployWithSlotOptions.IsActive)
        {
            return await Run(new DeployWithSlot(deployWithSlotOptions));
        }

        if (deployInplaceOptions.IsActive)
        {
            return await Run(new DeployInplace(deployInplaceOptions));
        }

        return 1;
    }

    private static async Task<int> Run(IBaseAction operation)
    {
        try
        {
            return await operation.RunAsync();
        }
        catch (Exception e)
        {
            throw e.Demystify();
        }
    }
}
