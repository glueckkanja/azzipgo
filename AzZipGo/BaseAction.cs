using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System.Threading.Tasks;

namespace AzZipGo
{
    public abstract class BaseAction<T> : IBaseAction where T : Options
    {
        protected const string SlotNamePrefix = "azzipgo-";

        public BaseAction(T options)
        {
            Options = options;
        }

        protected T Options { get; }

        protected AzureCredentials CreateAzureCredentials()
        {
            return new AzureCredentialsFactory().FromServicePrincipal(Options.User, Options.Password, Options.Tenant, Options.AzureEnvironment);
        }

        public abstract Task<int> RunAsync();
    }
}