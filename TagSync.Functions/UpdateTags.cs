using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using TagSync.Models;
using TagSync.Services;

namespace TagSync.Functions
{
    public static class UpdateTags
    {
        [FunctionName("UpdateTags")]
        public static async Task Run([QueueTrigger("resources-to-tag", Connection = "AzureWebJobsStorage")]string myQueueItem,
                    [Table("ResourceTypes")] CloudTable resourceTypesTable,
                    TraceWriter log
                )
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");
            ResourceItem updateItem = JsonConvert.DeserializeObject<ResourceItem>(myQueueItem);
            ResourceManagerService resourceManager = null;

            try
            {
                string token = AuthenticationService.GetAccessTokenAsync();
                resourceManager = new ResourceManagerService(token);
            }
            catch (Exception ex)
            {
                log.Error("Unable to connect to the ARM API, Message: " + ex.Message);
            }

            try
            {
                await resourceManager.UpdateResource(updateItem);
            }
            catch(Exception ex)
            {
                log.Error(updateItem.Id + " failed with: " + ex.Message);
                
                var resourceItemsQuery = await resourceTypesTable.ExecuteQuerySegmentedAsync(new TableQuery<ResourceType>(), null);
                var resourceType = resourceItemsQuery.Results.Where(x => x.Type == updateItem.Type).FirstOrDefault();

                if (resourceType != null)
                {
                    resourceType.ErrorMessage = ex.Message;
                    TableOperation insertOperation = TableOperation.InsertOrReplace(resourceType);
                    await resourceTypesTable.ExecuteAsync(insertOperation);
                }
            }
        }
    }
}
