using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Rest.Azure;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AzZipGo
{
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

            var latestDeployment = await GetLatestDeployment(ppTarget);

            var path = CreateZipFile();

            var upload = await PostFileAsync(ppTemp, path);

            if (upload.Code != HttpStatusCode.Accepted)
                return (int)upload.Code;

            File.Delete(path);

            var success = await WaitForCompleteAsync(ppTemp, latestDeployment, upload, true);

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
}
