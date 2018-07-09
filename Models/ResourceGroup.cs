using System;
using System.Collections.Generic;

namespace TagSync.Models
{
    public class ResourceGroup
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public IDictionary<string, string> Tags { get; set; }
    }
}
