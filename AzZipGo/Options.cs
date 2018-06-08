using CommandLine;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;

namespace AzZipGo
{
    public class Options
    {
        [Option('u', "user", Required = true)]
        public string User { get; set; }

        [Option('p', "password", Required = true)]
        public string Password { get; set; }

        [Option('t', "tenant", Required = true)]
        public string Tenant { get; set; }

        [Option(Default = AzureEnvironmentOption.Global, HelpText = "One of: Global, Germany, China, UsGov")]
        public AzureEnvironmentOption Environment { get; set; }

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
}