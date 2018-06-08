using CommandLine;

namespace AzZipGo
{
    public class DeployOptions : Options
    {
        [Option('s', "subscription", Required = true)]
        public string Subscription { get; set; }

        [Option('g', "resource-group", Required = true)]
        public string ResourceGroup { get; set; }

        [Option("site", Required = true)]
        public string Site { get; set; }

        [Option('d', "directory", Required = true)]
        public string Directory { get; set; }

        [Option("target-slot", Required = true)]
        public string TargetSlot { get; set; }
    }
}