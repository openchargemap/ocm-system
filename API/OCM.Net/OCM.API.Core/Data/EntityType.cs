using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class EntityType
    {
        public EntityType()
        {
            EditQueueItems = new HashSet<EditQueueItem>();
        }

        public short Id { get; set; }
        public string Title { get; set; }

        public virtual ICollection<EditQueueItem> EditQueueItems { get; set; }
    }
}
