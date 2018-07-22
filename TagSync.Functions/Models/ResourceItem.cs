using System.Collections.Generic;

namespace TagSync.Models
{
    public class ResourceItem
    {
        public string Id { get; set; }
        public string ApiVersion { get; set; }
        public string Location { get; set; }
        public IDictionary<string, string> Tags { get; set; }
        public string Type { get; set; }
        public string Subscription { get; set; }
    }
}