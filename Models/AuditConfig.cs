using Microsoft.WindowsAzure.Storage.Table;

namespace TagSync.Models
{
    public class AuditConfig : TableEntity
    {
        public string SubscriptionId { get; set; }
        public string RequiredTags { get; set; }
    }   
}