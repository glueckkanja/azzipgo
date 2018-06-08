using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AzZipGo
{
    public class DeployInplace : BaseDeployAction<DeployInplaceOptions>
    {
        public DeployInplace(DeployInplaceOptions options) : base(options)
        {
        }

        public override async Task<int> RunAsync()
        {
            var app = await GetSiteAsync();
            var ppTarget = await GetTargetPublishingProfileAsync(app);

            var latestDeployment = await GetLatestDeployment(ppTarget);

            var path = CreateZipFile();

            var upload = await PostFileAsync(ppTarget, path);

            if (upload.Code != HttpStatusCode.Accepted)
                return (int)upload.Code;

            File.Delete(path);

            var success = await WaitForCompleteAsync(ppTarget, latestDeployment, upload, false);

            Console.WriteLine();
            Console.WriteLine(success ? "Deployment succeeded." : "Deployment failed.");

            return success ? 0 : 100;
        }
    }
}