using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
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

            await ManageRunFromZipAsync();

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

        private async Task ManageRunFromZipAsync()
        {
            using (var mgmtClient = new WebSiteManagementClient(RestClient) { SubscriptionId = Options.Subscription })
            {
                Task<SlotConfigNamesResourceInner> slotNamesTask;
                Task<StringDictionaryInner> settingsTask;

                if (Options.TargetSlot != "production")
                {
                    slotNamesTask = mgmtClient.WebApps.ListSlotConfigurationNamesAsync(Options.ResourceGroup, Options.Site);
                    settingsTask = mgmtClient.WebApps.ListApplicationSettingsSlotAsync(Options.ResourceGroup, Options.Site, Options.TargetSlot);
                }
                else
                {
                    slotNamesTask = mgmtClient.WebApps.ListSlotConfigurationNamesAsync(Options.ResourceGroup, Options.Site);
                    settingsTask = mgmtClient.WebApps.ListApplicationSettingsAsync(Options.ResourceGroup, Options.Site);
                }

                var slotNames = await slotNamesTask;
                var settings = await settingsTask;

                if (Options.TargetSlot != "production")
                {
                    if (Options.RunFromPackage)
                    {
                        await MakeRunFromPackageNonStickyIfRequiredAsync(mgmtClient, slotNames);

                        if (settings.Properties.TryGetValue("WEBSITE_RUN_FROM_PACKAGE", out var value) && value != "1")
                        {
                            settings.Properties["WEBSITE_RUN_FROM_PACKAGE"] = "1";
                            await mgmtClient.WebApps.UpdateApplicationSettingsSlotAsync(Options.ResourceGroup, Options.Site, settings, Options.TargetSlot);
                            Console.WriteLine("Set application setting WEBSITE_RUN_FROM_PACKAGE to 1.");
                        }
                    }
                    else if (settings.Properties.TryGetValue("WEBSITE_RUN_FROM_PACKAGE", out var value))
                    {
                        settings.Properties.Remove("WEBSITE_RUN_FROM_PACKAGE");
                        await mgmtClient.WebApps.UpdateApplicationSettingsSlotAsync(Options.ResourceGroup, Options.Site, settings, Options.TargetSlot);
                        Console.WriteLine("Removed application setting WEBSITE_RUN_FROM_PACKAGE.");
                    }
                }
                else
                {
                    if (Options.RunFromPackage)
                    {
                        await MakeRunFromPackageNonStickyIfRequiredAsync(mgmtClient, slotNames);

                        if (settings.Properties.TryGetValue("WEBSITE_RUN_FROM_PACKAGE", out var value) && value != "1")
                        {
                            settings.Properties["WEBSITE_RUN_FROM_PACKAGE"] = "1";
                            await mgmtClient.WebApps.UpdateApplicationSettingsAsync(Options.ResourceGroup, Options.Site, settings);
                            Console.WriteLine("Set application setting WEBSITE_RUN_FROM_PACKAGE to 1.");
                        }
                    }
                    else if (settings.Properties.TryGetValue("WEBSITE_RUN_FROM_PACKAGE", out var value))
                    {
                        settings.Properties.Remove("WEBSITE_RUN_FROM_PACKAGE");
                        await mgmtClient.WebApps.UpdateApplicationSettingsAsync(Options.ResourceGroup, Options.Site, settings);
                        Console.WriteLine("Removed application setting WEBSITE_RUN_FROM_PACKAGE.");
                    }
                }
            }
        }

        private async Task MakeRunFromPackageNonStickyIfRequiredAsync(WebSiteManagementClient mgmtClient, SlotConfigNamesResourceInner slotNames)
        {
            if (!slotNames.AppSettingNames.Contains("WEBSITE_RUN_FROM_PACKAGE"))
                return;

            slotNames.AppSettingNames.Remove("WEBSITE_RUN_FROM_PACKAGE");
            await mgmtClient.WebApps.UpdateSlotConfigurationNamesAsync(Options.ResourceGroup, Options.Site, slotNames);
            Console.WriteLine("Removed application setting WEBSITE_RUN_FROM_PACKAGE from list of slot settings.");
        }
    }
}