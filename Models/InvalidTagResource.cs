using Microsoft.WindowsAzure.Storage.Table;

namespace TagSync.Models
{
    public class InvalidTagResource : TableEntity
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }
}