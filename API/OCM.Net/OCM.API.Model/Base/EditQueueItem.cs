using System;
using System.Collections.Generic;

namespace OCM.API.Common.Model
{
    public class EditQueueItem
    {
        public int ID { get; set; }
        public User User { get; set; }
        public string Comment { get; set; }
        public bool IsProcessed { get; set; }
        public User ProcessedByUser { get; set; }
        public DateTime DateSubmitted { get; set; }
        public DateTime? DateProcessed { get; set; }
        public string EditData { get; set; }
        public string PreviousData { get; set; }
        public int? EntityID { get; set; }
        public EntityType EntityType { get; set; }

#if !PORTABLE
        //extended properties
        public List<DiffItem> Differences { get; set; }
#endif
    }
}