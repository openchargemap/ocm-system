using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class EditQueueItem
    {
        public int ID { get; set; }
        public Nullable<int> UserID { get; set; }
        public string Comment { get; set; }
        public bool IsProcessed { get; set; }
        public Nullable<int> ProcessedByUserID { get; set; }
        public System.DateTime DateSubmitted { get; set; }
        public Nullable<System.DateTime> DateProcessed { get; set; }
        public string EditData { get; set; }
        public string PreviousData { get; set; }
        public Nullable<int> EntityID { get; set; }
        public Nullable<short> EntityTypeID { get; set; }
        public virtual User User { get; set; }
        public virtual User ProcessedByUser { get; set; }
        public virtual EntityType EntityType { get; set; }
    }
}
