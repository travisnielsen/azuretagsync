using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Newtonsoft.Json; 
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TagSync.Models;
using TagSync.Services;

namespace TagSync.Functions
{
    public static class AuditTags
    {
        static CloudTable _resourceTypesTbl;
        static List<ResourceType> _resourceTypes;
        static TraceWriter _log;
        static ResourceManagerService _resourceManager;

        [FunctionName("AuditTags")]
        public static async Task Run(
            [TimerTrigger("0 0 */4 * * *", RunOnStartup = false )]TimerInfo timer, 
            [Table("AuditConfig")] CloudTable configTbl,
            [Table("AuditStats")] CloudTable statsTbl,
            [Table("ResourceTypes")] CloudTable resourceTypesTbl,
            [Queue("resources-to-tag")] ICollector<string> outQueue,
            TraceWriter log )
        {
            _log = log;
            _resourceTypesTbl = resourceTypesTbl;
            log.Info("Starding subscription audit.");

            var resourceTypesQuery = await resourceTypesTbl.ExecuteQuerySegmentedAsync(new TableQuery<ResourceType>(), null);
            _resourceTypes = resourceTypesQuery.Results;
            var auditConfigQuery = await configTbl.ExecuteQuerySegmentedAsync(new TableQuery<AuditConfig>(), null);

            // Init config table if new deployment
            if (auditConfigQuery.Results.Count == 0)
            {
                AuditConfig initConfig = new AuditConfig { SubscriptionId = "enter_valid_subscription_id", RowKey = Guid.NewGuid().ToString(), RequiredTags = "comma,separated,tag,list,here", PartitionKey = "init" };
                TableOperation insertOperation = TableOperation.InsertOrReplace(initConfig);
                await configTbl.ExecuteAsync(insertOperation);
                log.Info("First run for new deployment. Please populate the AuditConfig table.");
                return;
            }

            foreach (var auditConfig in auditConfigQuery.Results)
            {
                try
                {
                    AuditStats stats = new AuditStats { JobStart= DateTime.Now, PartitionKey = auditConfig.SubscriptionId, RowKey = Guid.NewGuid().ToString() };
                    IEnumerable<string> requiredTagsList = auditConfig.RequiredTags.Split(',');

                    try
                    {
                        string token = AuthenticationService.GetAccessTokenAsync();
                        _resourceManager = new ResourceManagerService(token);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Unable to connect to the ARM API, Message: " + ex.Message);
                    }

                    List<ResourceItem> tagUpdates = await ProcessResourceGroups(requiredTagsList, auditConfig.SubscriptionId, stats);

                    foreach(ResourceItem resourceItem in tagUpdates)
                    {
                        string messageText = JsonConvert.SerializeObject(resourceItem);
                        _log.Info("Requesting tags for: " + resourceItem.Id);
                        outQueue.Add(messageText);
                    }

                    log.Info("Completed audit of subscription: " + auditConfig.SubscriptionId);
                    stats.JobEnd = DateTime.Now;

                    TableOperation insertOperation = TableOperation.InsertOrReplace(stats);
                    await statsTbl.ExecuteAsync(insertOperation);
                }
                catch (Exception ex)
                {
                    log.Error("Failure processing resource groups for auditConfig: " + auditConfig.RowKey);
                    log.Error(ex.Message);
                }
            }
        }

        static async Task<List<ResourceItem>> ProcessResourceGroups(IEnumerable<string> requiredTagsList, string subscriptionId, AuditStats stats )
        {
            List<ResourceItem> updateList = new List<ResourceItem>();
            List<string> invalidResourceTypes = _resourceTypes.Where(t => !String.IsNullOrEmpty(t.ErrorMessage)).Select(t => t.Type).ToList();
            var resourceGroups = await _resourceManager.GetResourceGroups(subscriptionId);
            stats.ResourceGroupsTotal = resourceGroups.Count;

            foreach (var rg in resourceGroups)
            {
                _log.Info("Resource group: " + rg.Name);

                if (rg.Tags == null)
                {
                    _log.Warning("Resource group: " + rg.Name + " does not have tags.");
                    continue;
                }

                var tagsToSync = TagService.GetRequiredTags((Dictionary<string, string>)rg.Tags, requiredTagsList);

                if (tagsToSync.Count < 1)
                { 
                    _log.Warning("Resource group: " + rg.Name + " does not have required tags.");
                    stats.ResourceGroupsSkipped += 1;
                }
                else
                {
                    List<ResourceItem> resources = await _resourceManager.GetResources(rg.Name, subscriptionId, invalidResourceTypes);
                    stats.ResourceItemsTotal = resources.Count();

                    foreach(var resource in resources)
                    {
                        var result = TagService.GetTagUpdates(resource.Tags.ToDictionary(x => x.Key, x => x.Value), tagsToSync);

                        if (result.Count > 0)
                        {
                            try
                            {
                                stats.ResourceItemsWithUpdates += 1;
                                resource.Tags = result;
                                resource.ApiVersion = await GetApiVersion(resource);
                                updateList.Add(resource);
                            }
                            catch(Exception ex)
                            {
                                _log.Error("Failure processing resource: " + resource.Id);
                                _log.Error(ex.Message);
                            }
                        }
                        else
                        {
                            stats.ResourceItemsSkipped += 1;
                        }
                    }
                }
            }

            return updateList;
        }

        static async Task<string> GetApiVersion(ResourceItem resource)
        {
            ResourceType matchingItem = _resourceTypes.Where(r => r.Type == resource.Type).FirstOrDefault();

            if(matchingItem != null)
            {
                _log.Verbose("API version found in ResourceTypes table");
                return matchingItem.ApiVersion;
            }
            else
            {
                string apiVersion = await _resourceManager.GetApiVersion(resource.Type);
                if (!String.IsNullOrEmpty(apiVersion))
                {
                    _log.Verbose("Got API version from resource maanger service");
                    await AddResourceType(resource, apiVersion);
                    return apiVersion;
                }
                else
                    throw new Exception("Unable to get API version");
            }
        }

        static async Task AddResourceType(ResourceItem resource, string apiVersion)
        {
            var newResourceType = new ResourceType { ApiLocation = resource.Location, ApiVersion = apiVersion, Type = resource.Type, RowKey = Guid.NewGuid().ToString(), PartitionKey = "tagsync" };
            _resourceTypes.Add(newResourceType);

            TableOperation insertOperation = TableOperation.InsertOrReplace(newResourceType);
            await _resourceTypesTbl.ExecuteAsync(insertOperation);
        }
    }
}
