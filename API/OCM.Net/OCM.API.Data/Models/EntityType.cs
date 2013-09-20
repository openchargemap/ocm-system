using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class EntityType
    {
        public EntityType()
        {
            this.EditQueueItems = new List<EditQueueItem>();
        }

        public short ID { get; set; }
        public string Title { get; set; }
        public virtual ICollection<EditQueueItem> EditQueueItems { get; set; }
    }
}
