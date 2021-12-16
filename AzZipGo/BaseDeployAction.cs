﻿using AzZipGo.Kudu.Api;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzZipGo;

public abstract class BaseDeployAction<T> : BaseAction<T> where T : DeployOptions
{
    private static readonly AsyncRetryPolicy HttpPolicy = Policy
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) => Console.WriteLine($"HTTP retry {retryCount}: {exception.Demystify()}."));

    public BaseDeployAction(T options) : base(options)
    {
        RestClient = RestClient.Configure().WithEnvironment(Options.AzureEnvironment).WithCredentials(CreateAzureCredentials()).Build();
        AzureApi = Azure.Authenticate(RestClient, Options.Tenant).WithSubscription(Options.Subscription);
    }

    public RestClient RestClient { get; set; }
    public IAzure AzureApi { get; set; }

    public string SiteId => $"/subscriptions/{Options.Subscription}/resourceGroups/{Options.ResourceGroup}/providers/Microsoft.Web/sites/{Options.Site}";

    protected async Task<bool> WaitForCompleteAsync(IPublishingProfile ppSlot, Deployment latestDeployment, Uri pollUrl, bool withSwap)
    {
        Console.Write($"Waiting for deployment to complete...");

        var success = false;

        var handler = new HttpClientHandler { Credentials = new NetworkCredential(ppSlot.GitUsername, ppSlot.GitPassword) };
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(30) };

        var kuduDeployCompleted = false;

        Deployment? deployment = null;

        for (int i = 0; i < 600; i++)
        {
            deployment = await GetDeploymentAsync(http, pollUrl);

            if (deployment.status == DeployStatus.Failed)
            {
                Console.WriteLine("SCM deployment failed.");
                return false;
            }

            if (deployment.status == DeployStatus.Success && !kuduDeployCompleted)
            {
                kuduDeployCompleted = true;

                if (withSwap)
                {
                    Console.WriteLine();
                    Console.WriteLine("SCM deployment completed. Waiting for auto-swap to complete.");
                    Console.Write("...");
                }
            }

            if (withSwap)
            {
                if (deployment.id == latestDeployment.id)
                {
                    Console.WriteLine();
                    success = true;
                    break;
                }
            }
            else
            {
                if (deployment.id != latestDeployment.id)
                {
                    Console.WriteLine();
                    success = true;
                    break;
                }
            }

            await Task.Delay(500);
            Console.Write(".");
        }

        if (!success)
            Console.WriteLine();

        return success;
    }

    protected async Task<(HttpStatusCode Code, Uri PollUrl)> PostFileAsync(IPublishingProfile ppSlot, string path)
    {
        var handler = new HttpClientHandler { Credentials = new NetworkCredential(ppSlot.GitUsername, ppSlot.GitPassword) };
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(30) };

        var slotHost = ppSlot.GitUrl.Split(':')[0];
        var zipDeployUrl = new UriBuilder() { Scheme = "https", Host = slotHost, Path = "/api/zipdeploy", Query = "isAsync=true" }.ToString();

        using (var fs = File.OpenRead(path))
        {
            var (code, pollUrl) = await HttpPolicy.ExecuteAsync(async () =>
            {
                Console.WriteLine($"HTTP: POST {fs.Length / 1024.0:f1} KiB to {zipDeployUrl}");

                using (var response = await http.PostAsync(zipDeployUrl, new StreamContent(fs)))
                {
                    var pollUrl = response.Headers.Location;

                    Console.WriteLine("  > " + response.StatusCode);

                    return (response.StatusCode, pollUrl);
                }
            });

            return (code, pollUrl);
        }
    }

    protected async Task<IPublishingProfile> GetTargetPublishingProfileAsync(IWebApp app)
    {
        var targetSlot = await FindTargetSlotAndCleanOldSlotsAsync(app);

        if (targetSlot != null)
        {
            Console.WriteLine($"Retrieving publishing profile of slot {targetSlot.Name}...");
            return await targetSlot.GetPublishingProfileAsync();
        }

        Console.WriteLine($"Retrieving publishing profile of production slot...");
        return await app.GetPublishingProfileAsync();
    }

    protected static async Task<Deployment> GetDeploymentAsync(HttpClient http, Uri pollUrl)
    {
        Deployment deployment;

        using (var response = await http.GetAsync(pollUrl))
        {
            var text = "No Response";

            try
            {
                text = await response.Content.ReadAsStringAsync();
                deployment = JObject.Parse(text).ToObject<Deployment>();
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine("Unable to parse 'Deployment'!");
                Console.WriteLine("Response was:");
                Console.WriteLine(text);
                Console.WriteLine("Exception:");
                Console.WriteLine(e.Demystify());
                Console.WriteLine();

                deployment = new Deployment { id = "0000000000000000000000000000000000000000" };
            }
        }

        return deployment;
    }

    protected static async Task<Deployment> GetLatestDeployment(IPublishingProfile pp)
    {
        var handler = new HttpClientHandler { Credentials = new NetworkCredential(pp.GitUsername, pp.GitPassword) };
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(30) };

        var appHost = pp.GitUrl.Split(':')[0];
        var appDeploymentUrl = new UriBuilder() { Scheme = "https", Host = appHost, Path = "/api/deployments/latest" }.ToString();

        Deployment latestDeployment;

        using (var response = await http.GetAsync(appDeploymentUrl))
        {
            try
            {
                latestDeployment = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<Deployment>();
            }
            catch
            {
                latestDeployment = new Deployment { id = "0000000000000000000000000000000000000000" };
            }
        }

        Console.WriteLine($"Latest deployment at {(latestDeployment.last_success_end_time?.ToString("u") ?? "[no timestamp]")} has ID {latestDeployment.id}");
        return latestDeployment;
    }

    protected string CreateZipFile()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"azzipgo-{Guid.NewGuid()}.zip");

        Console.WriteLine($"Creating zip file...");
        var sw = Stopwatch.StartNew();

        ZipFile.CreateFromDirectory(Options.Directory, temp, CompressionLevel.Fastest, false);

        Console.WriteLine($"Completed! Creating zip file took {sw.ElapsedMilliseconds} ms.");
        return temp;
    }

    protected async Task<IWebApp> GetSiteAsync()
    {
        Console.WriteLine($"Getting site {Options.Site}");
        var app = await AzureApi.WebApps.GetByIdAsync(SiteId);
        return app;
    }

    protected async Task ManageRunFromZipAsync(string slot)
    {
        using (var mgmtClient = new WebSiteManagementClient(RestClient) { SubscriptionId = Options.Subscription })
        {
            Task<SlotConfigNamesResourceInner> slotNamesTask;
            Task<StringDictionaryInner> settingsTask;

            if (slot != "production")
            {
                slotNamesTask = mgmtClient.WebApps.ListSlotConfigurationNamesAsync(Options.ResourceGroup, Options.Site);
                settingsTask = mgmtClient.WebApps.ListApplicationSettingsSlotAsync(Options.ResourceGroup, Options.Site, slot);
            }
            else
            {
                slotNamesTask = mgmtClient.WebApps.ListSlotConfigurationNamesAsync(Options.ResourceGroup, Options.Site);
                settingsTask = mgmtClient.WebApps.ListApplicationSettingsAsync(Options.ResourceGroup, Options.Site);
            }

            var slotNames = await slotNamesTask;
            var settings = await settingsTask;

            if (slot != "production")
            {
                if (Options.RunFromPackage)
                {
                    await MakeRunFromPackageNonStickyIfRequiredAsync(mgmtClient, slotNames);

                    if (!settings.Properties.TryGetValue("WEBSITE_RUN_FROM_PACKAGE", out var value) || value != "1")
                    {
                        settings.Properties["WEBSITE_RUN_FROM_PACKAGE"] = "1";
                        await mgmtClient.WebApps.UpdateApplicationSettingsSlotAsync(Options.ResourceGroup, Options.Site, settings, slot);
                        Console.WriteLine($"Set application setting WEBSITE_RUN_FROM_PACKAGE for slot {slot} to 1.");
                    }
                }
                else if (settings.Properties.TryGetValue("WEBSITE_RUN_FROM_PACKAGE", out var value))
                {
                    if (value == "1")
                    {
                        // Needs to be disabled first.
                        settings.Properties["WEBSITE_RUN_FROM_PACKAGE"] = "0";
                        await mgmtClient.WebApps.UpdateApplicationSettingsSlotAsync(Options.ResourceGroup, Options.Site, settings, slot);
                        Console.WriteLine($"Set application setting WEBSITE_RUN_FROM_PACKAGE for slot {slot} to 0.");
                    }
                    else
                    {
                        settings.Properties.Remove("WEBSITE_RUN_FROM_PACKAGE");
                        await mgmtClient.WebApps.UpdateApplicationSettingsSlotAsync(Options.ResourceGroup, Options.Site, settings, slot);
                        Console.WriteLine($"Removed application setting WEBSITE_RUN_FROM_PACKAGE from slot {slot}.");
                    }
                }
            }
            else
            {
                if (Options.RunFromPackage)
                {
                    await MakeRunFromPackageNonStickyIfRequiredAsync(mgmtClient, slotNames);

                    if (!settings.Properties.TryGetValue("WEBSITE_RUN_FROM_PACKAGE", out var value) || value != "1")
                    {
                        settings.Properties["WEBSITE_RUN_FROM_PACKAGE"] = "1";
                        await mgmtClient.WebApps.UpdateApplicationSettingsAsync(Options.ResourceGroup, Options.Site, settings);
                        Console.WriteLine($"Set application setting WEBSITE_RUN_FROM_PACKAGE for slot {slot} to 1.");
                    }
                }
                else if (settings.Properties.TryGetValue("WEBSITE_RUN_FROM_PACKAGE", out var value))
                {
                    if (value == "1")
                    {
                        // Needs to be disabled first.
                        settings.Properties["WEBSITE_RUN_FROM_PACKAGE"] = "0";
                        await mgmtClient.WebApps.UpdateApplicationSettingsAsync(Options.ResourceGroup, Options.Site, settings);
                        Console.WriteLine($"Set application setting WEBSITE_RUN_FROM_PACKAGE for slot {slot} to 0.");
                    }
                    else
                    {
                        settings.Properties.Remove("WEBSITE_RUN_FROM_PACKAGE");
                        await mgmtClient.WebApps.UpdateApplicationSettingsAsync(Options.ResourceGroup, Options.Site, settings);
                        Console.WriteLine($"Removed application setting WEBSITE_RUN_FROM_PACKAGE from slot {slot}.");
                    }
                }
            }
        }
    }

    private async Task MakeRunFromPackageNonStickyIfRequiredAsync(WebSiteManagementClient mgmtClient, SlotConfigNamesResourceInner slotNames)
    {
        if (slotNames.AppSettingNames?.Contains("WEBSITE_RUN_FROM_PACKAGE") != true)
            return;

        slotNames.AppSettingNames.Remove("WEBSITE_RUN_FROM_PACKAGE");
        await mgmtClient.WebApps.UpdateSlotConfigurationNamesAsync(Options.ResourceGroup, Options.Site, slotNames);
        Console.WriteLine("Removed application setting WEBSITE_RUN_FROM_PACKAGE from list of slot settings.");
    }

    private async Task<IDeploymentSlot?> FindTargetSlotAndCleanOldSlotsAsync(IWebApp app)
    {
        IDeploymentSlot? slot = null;

        // clean old deployments
        foreach (var oldSlot in await app.DeploymentSlots.ListAsync())
        {
            if (oldSlot.Name == Options.TargetSlot)
            {
                slot = oldSlot;
                continue;
            }

            if (oldSlot.Name.StartsWith(SlotNamePrefix + Options.TargetSlot + "-"))
            {
                Console.WriteLine($"Deleting old temporary slot {oldSlot.Name}...");

                try
                {
                    await app.DeploymentSlots.DeleteByIdAsync(oldSlot.Id);
                }
                catch (CloudException e) when (e.Body.Code == "Conflict")
                {
                }
            }
        }

        if (slot != null)
            Console.WriteLine($"Using deployment slot {slot.Name} as target...");
        else
            Console.WriteLine($"Using production slot as target...");

        return slot;
    }
}
