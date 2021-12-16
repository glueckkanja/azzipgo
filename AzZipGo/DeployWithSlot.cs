using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Rest.Azure;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AzZipGo;

public class DeployWithSlot : BaseDeployAction<DeployWithSlotOptions>
{
    public DeployWithSlot(DeployWithSlotOptions options) : base(options)
    {
    }

    public override async Task<int> RunAsync()
    {
        var app = await GetSiteAsync();
        var ppTarget = await GetTargetPublishingProfileAsync(app);

        var slotTemp = await CreateNewTempSlotAsync(app);
        var ppTemp = await slotTemp.GetPublishingProfileAsync();

        await ManageRunFromZipAsync(slotTemp.Name);

        var latestDeployment = await GetLatestDeployment(ppTarget);

        var path = CreateZipFile();

        var (code, pollUrl) = await PostFileAsync(ppTemp, path);

        if (code != HttpStatusCode.Accepted)
            return (int)code;

        if (pollUrl is null)
            return 9999;

        File.Delete(path);

        var success = await WaitForCompleteAsync(ppTemp, latestDeployment, pollUrl, true);

        Console.WriteLine();

        if (success)
        {
            Console.WriteLine("Deployment succeeded.");

            if (Options.CleanupAfterSuccess)
            {
                await RunCleanupAsync(app, slotTemp);
            }
        }
        else if (Options.CleanupAfterSuccess)
        {
            Console.WriteLine("Deployment failed. Running no clean up. Please check both deployment slots.");
        }
        else
        {
            Console.WriteLine("Deployment failed.");
        }

        return success ? 0 : 100;
    }

    private async Task<IDeploymentSlot> CreateNewTempSlotAsync(IWebApp app)
    {
        var slotName = SlotNamePrefix + Options.TargetSlot + "-" + Guid.NewGuid().ToString().Substring(0, 8);

        Console.WriteLine($"Creating temporary slot {slotName}...");

        var request = app.DeploymentSlots.Define(slotName)
            .WithConfigurationFromParent()
            .WithAutoSwapSlotName(Options.TargetSlot);

        if (Options.StopWebjobs)
        {
            request.WithStickyAppSetting("WEBJOBS_STOPPED", "1");
        }

        return await request.CreateAsync();
    }

    private async Task RunCleanupAsync(IWebApp app, IDeploymentSlot slot)
    {
        Console.WriteLine($"Will delete temporary slot {slot.Name} in 2 minutes...");

        await Task.Delay(TimeSpan.FromMinutes(2));

        Console.WriteLine($"Deleting temporary slot {slot.Name}...");

        try
        {
            await app.DeploymentSlots.DeleteByIdAsync(slot.Id);
        }
        catch (CloudException e) when (e.Body.Code == "Conflict")
        {
        }
    }
}
