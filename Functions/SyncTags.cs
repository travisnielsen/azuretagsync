using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using TagSync.Models;
using TagSync.Services;

namespace TagSync
{
    public static class SyncTags
    {
        [FunctionName("SyncTags")]
        public static async Task Run([QueueTrigger("resources-to-tag", Connection = "AzureWebJobsStorage")]string myQueueItem,
                    [Table("ResourceTypes")] CloudTable invalidResourceTable,
                    TraceWriter log
                )
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");
            ResourceItem updateItem = JsonConvert.DeserializeObject<ResourceItem>(myQueueItem);
            ResourceManagerService resourceManager = null;

            try
            {
                string token = await AuthenticationService.GetAccessTokenAsync();
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
                
                InvalidTagResource matchingInvalidResource = null;
                var invalidTagResourcesQuery = await invalidResourceTable.ExecuteQuerySegmentedAsync(new TableQuery<InvalidTagResource>(), null);

                if (invalidTagResourcesQuery.Results != null)
                    matchingInvalidResource = invalidTagResourcesQuery.Results.Where(x => x.Type == updateItem.Type).FirstOrDefault();

                if (matchingInvalidResource == null)
                {
                    InvalidTagResource invalidItem = new InvalidTagResource
                    { 
                        Type = updateItem.Type, 
                        Message = ex.Message,
                        RowKey = Guid.NewGuid().ToString(),
                        PartitionKey = updateItem.Subscription
                    };

                    TableOperation insertOperation = TableOperation.InsertOrReplace(invalidItem);
                    await invalidResourceTable.ExecuteAsync(insertOperation);
                }
            }
        }
    }
}
