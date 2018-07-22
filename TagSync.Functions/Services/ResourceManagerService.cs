using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Rest;
using TagSync.Models;

namespace TagSync.Services
{
    public class ResourceManagerService
    {
        static Dictionary<string, string> _apiVersions = new Dictionary<string, string>();
        ResourceManagementClient _client;


        public ResourceManagerService(string accessToken)
        {
            _client = new ResourceManagementClient(new TokenCredentials(accessToken));
        }

        public async Task<List<ResourceGroup>> GetResourceGroups(string subscriptionId)
        {
            _client.SubscriptionId = subscriptionId;
            IEnumerable<ResourceGroupInner> resourceGroups = await _client.ResourceGroups.ListAsync();

            List<ResourceGroup> rgs = new List<ResourceGroup>();
            foreach (var item in resourceGroups)
            {
                rgs.Add(new ResourceGroup { Name = item.Name, Tags = item.Tags });
            }

            return rgs;
        }


        public async Task<List<ResourceItem>> GetResources(string resourceGroupName, string subscriptionId, List<string> invalidTypes)
        {
            List<ResourceItem> resourceList = new List<ResourceItem>();
            IEnumerable<GenericResourceInner> resources = await _client.Resources.ListByResourceGroupAsync(resourceGroupName);
            var invalidItems = resources.Where(x => invalidTypes.Contains(x.Type));
            var filteredResources = resources.Except(invalidItems);

            foreach (var item in filteredResources)
            {
                Dictionary<string, string> itemTags = new Dictionary<string, string>();
                if (item.Tags != null)
                {
                    itemTags = item.Tags.ToDictionary(x => x.Key, x=> x.Value);
                }

                resourceList.Add(new ResourceItem { Id = item.Id, Subscription = subscriptionId, Location = item.Location, Type = item.Type, Tags = itemTags, ApiVersion = await GetApiVersion(item.Type) });   
            }

            return resourceList;
        }

        public async Task UpdateResource(ResourceItem updateItem)
        {
            _client.SubscriptionId = updateItem.Subscription;
            GenericResourceInner resource  = await _client.Resources.GetByIdAsync(updateItem.Id, updateItem.ApiVersion);
            resource.Tags = updateItem.Tags;
            resource.Properties = null;  // some resource types support PATCH operations ONLY on tags.
            await _client.Resources.UpdateByIdAsync(updateItem.Id, updateItem.ApiVersion, resource);
        }


        async Task<string> GetApiVersion(string type)
        {
            string resourceProvider = type.Split('/')[0];
            string resourceType = type.TrimStart(resourceProvider.ToCharArray()).TrimStart('/');

            if (_apiVersions.ContainsKey(resourceType))
                return _apiVersions[resourceType];

            try
            {
                ProviderInner provider = await _client.Providers.GetAsync(resourceProvider);
                var resourceApi = provider.ResourceTypes.Where(x => x.ResourceType == resourceType).FirstOrDefault().ApiVersions[0];
                _apiVersions.Add(resourceType, resourceApi);
                return resourceApi;
            }
            catch (Exception ex) { throw (ex); }

        }

    }
}
