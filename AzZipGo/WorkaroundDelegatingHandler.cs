using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzZipGo
{
    public class WorkaroundDelegatingHandler : DelegatingHandlerBase
    {
        private readonly DeployOptions options;

        public WorkaroundDelegatingHandler(DeployOptions options)
        {
            this.options = options;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // This is a workaround for https://github.com/Azure/azure-libraries-for-net/issues/435

            var expectedPathAndId = $"/subscriptions/{options.Subscription}/resourceGroups/{options.ResourceGroup}/providers/Microsoft.Web/sites/{options.Site}/slots";

            if (request.RequestUri.AbsolutePath == expectedPathAndId)
            {
                using (var response = await base.SendAsync(request, cancellationToken))
                using (var content = await response.Content.ReadAsStreamAsync())
                {
                    var json = await LoadJObjectAsync(content);

                    if (json["value"] is JArray slots)
                    {
                        foreach (var slot in slots)
                        {
                            if (slot["properties"] is JObject properties)
                            {
                                if ((string)properties["resourceGroup"] != null)
                                    continue;

                                var id = (string)slot["id"];

                                // Test for expected ID
                                if (id.StartsWith(expectedPathAndId))
                                {
                                    properties["resourceGroup"] = options.ResourceGroup;
                                }
                            }
                        }
                    }

                    var cloneContent = new StringContent(json.ToString(Formatting.None), Encoding.UTF8, "application/json");

                    foreach (var header in response.Content.Headers)
                        cloneContent.Headers.TryAddWithoutValidation(header.Key, header.Value);

                    var clone = new HttpResponseMessage(response.StatusCode) { Content = cloneContent };

                    foreach (var header in response.Headers)
                        clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

                    return clone;
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<JObject> LoadJObjectAsync(Stream stream)
        {
            using (var r1 = new StreamReader(stream))
            using (var r2 = new JsonTextReader(r1))
            {
                return await JObject.LoadAsync(r2);
            }
        }
    }
}