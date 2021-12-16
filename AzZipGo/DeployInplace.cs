using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AzZipGo;

public class DeployInplace : BaseDeployAction<DeployInplaceOptions>
{
    public DeployInplace(DeployInplaceOptions options) : base(options)
    {
    }

    public override async Task<int> RunAsync()
    {
        var app = await GetSiteAsync();
        var ppTarget = await GetTargetPublishingProfileAsync(app);
        using var ppTargetClient = CreateHttpClient(ppTarget);

        await ManageRunFromZipAsync(Options.TargetSlot);

        var latestDeployment = await GetLatestDeployment(ppTarget, ppTargetClient);

        var path = CreateZipFile();

        var (code, pollUrl) = await PostFileAsync(ppTarget, ppTargetClient, path);

        if (code != HttpStatusCode.Accepted)
            return (int)code;

        if (pollUrl is null)
            return 9999;

        File.Delete(path);

        var success = await WaitForCompleteAsync(ppTargetClient, latestDeployment, pollUrl, false);

        Console.WriteLine();
        Console.WriteLine(success ? "Deployment succeeded." : "Deployment failed.");

        return success ? 0 : 100;
    }
}
