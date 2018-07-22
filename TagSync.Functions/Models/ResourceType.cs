using Microsoft.WindowsAzure.Storage.Table;

namespace TagSync.Models
{
    public class ResourceType : TableEntity
    {
        public string Type { get; set; }
        public string ApiVersion { get; set; }
        public string ApiLocation { get; set; }
        public string PropertyExclude { get; set; }
        public string ErrorMessage { get; set; }
    }
}