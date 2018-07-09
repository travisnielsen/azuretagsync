using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace TagSync.Models
{
    public class AuditStats : TableEntity
    {
        public DateTime JobStart { get; set; }
        public DateTime JobEnd { get; set; }
        public int ResourceGroupsTotal { get; set; } = 0;
        public int ResourceGroupsSkipped { get; set; } = 0;
        public int ResourceItemsTotal { get; set; } = 0;
        public int ResourceItemsSkipped { get; set; } = 0;
        public int ResourceItemsWithUpdates { get; set; } = 0;
    }   
}