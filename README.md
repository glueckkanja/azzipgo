# AzZipGo – Azure ☁️ Zip 📦 and Go 🚀

* **Deploy all the things**: Azure Websites, Function Apps and WebJobs!
* Runs **everywhere**
* Uses Kudu's **ZipDeploy** feature
* Uses an auto-generated **Deployment Slot and Auto-Swap** when using the _deploy-with-slot_ command
* Or deploys **directly to a WebSite** slot using the _deploy-in-place_ command

## Usage

```azzipgo COMMAND [OPTIONS]+```

### COMMAND deploy-with-slot

Deploy Site using ZipDeploy and a newly created slot.

* cleanup-after-success -- Delete temporary slot after deployment. This will add a wait period of 2 minutes. Default = false
* stop-webjobs -- Set sticky app setting WEBJOBS_STOPPED=1 for the new temporary slot. Default = true

### COMMAND deploy-in-place

Deploy Site using ZipDeploy and run the deployment in-place without a temporary slot and auto-swap.

### Generic Options

* s|subscription -- The subscription ID
* g|resource-group -- The resource group of the website
* d|directory -- The path to the directory to deploy
* site -- The site name to deploy to
* target-slot -- The slot name to deploy to. Use *production* to deploy to the specified website directly.
* run-from-package -- Set or remove app setting WEBSITE_RUN_FROM_PACKAGE for the target or temp slot. This will enable or disable the Run From Package feature. Default = false
