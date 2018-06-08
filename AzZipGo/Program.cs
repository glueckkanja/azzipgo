using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AzZipGo
{
    public class Program
    {
        private static ParserResult<object> result;

        private static async Task<int> Main(string[] args)
        {
            result = Parser.Default.ParseArguments<DeployWithSlotOptions, DeployInplaceOptions>(args);

            return await result.MapResult(
                async (DeployWithSlotOptions opts) => await Run(opts),
                async (DeployInplaceOptions opts) => await Run(opts),
                async errs => await HandleParseErrorAsync(errs));
        }

        private static async Task<int> Run(Options opts)
        {
            IBaseAction operation;

            if (opts is DeployWithSlotOptions a)
                operation = new DeployWithSlot(a);
            else if (opts is DeployInplaceOptions b)
                operation = new DeployInplace(b);
            else
                throw new Exception("Unknown operation");

            try
            {
                return await operation.RunAsync();
            }
            catch (Exception e)
            {
                throw e.Demystify();
            }
        }

        private static Task<int> HandleParseErrorAsync(IEnumerable<Error> errs)
        {
            return Task.FromResult(1);
        }
    }
}
